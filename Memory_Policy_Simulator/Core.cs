using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_Policy_Simulator
{
    class Core
    {        private int cursor;
        public int p_frame_size;
        public List<Page> frame_window;
        public Queue<Page> fifo_frame_window;
        public List<Page> pageHistory;
        // 각 Operate 후 프레임 상태를 저장 (시각화 용)
        public List<List<char>> framesHistory;
        
        // LFU를 위한 페이지 참조 빈도 추적용 딕셔너리
        public Dictionary<char, int> pageFrequency;
        // 순수 LFU의 tie-break를 위한 페이지 최초 삽입 시간 기록
        public Dictionary<char, int> pageInsertionTime;
        // 현재 시간 (페이지 처리 횟수)
        public int currentTime;

        public int hit;
        public int fault;
        public int migration;
          // 페이지 교체 정책
        public enum POLICY { FIFO, LRU, LFU }
        public POLICY policy;        public Core(int get_frame_size, POLICY policy = POLICY.LRU)
        {
            this.p_frame_size = get_frame_size;
            this.policy = policy;
            this.pageHistory = new List<Page>();
            this.framesHistory = new List<List<char>>();
            
            // 정책에 따른 자료구조 초기화
            if (policy == POLICY.FIFO)
            {
                this.fifo_frame_window = new Queue<Page>();
            }
            else // LRU, LFU
            {
                this.frame_window = new List<Page>();
                  // LFU 전용 초기화
                if (policy == POLICY.LFU)
                {
                    this.pageFrequency = new Dictionary<char, int>();
                    this.pageInsertionTime = new Dictionary<char, int>();
                    this.currentTime = 0;
                }
            }
        }        public Page.STATUS Operate(char data)
        {
            if (policy == POLICY.FIFO)
                return OperateFIFO(data);
            else if (policy == POLICY.LRU)
                return OperateLRU(data);
            else
                return OperateLFU(data);
        }

        private Page.STATUS OperateFIFO(char data)
        {
            Page newPage;            if (this.fifo_frame_window.Any<Page>(x => x.data == data))
            {
                newPage.pid = Page.CREATE_ID++;
                newPage.data = data;
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                int i;

                for (i = 0; i < this.fifo_frame_window.Count; i++)
                {
                    if (this.fifo_frame_window.ElementAt(i).data == data) break;
                }
                // Hit가 발생한 페이지의 위치를 저장 (실제 프레임 내 위치 + 1)
                newPage.loc = i + 1;
            }
            else
            {
                newPage.pid = Page.CREATE_ID++;
                newPage.data = data;

                if (fifo_frame_window.Count >= p_frame_size)
                {
                    newPage.status = Page.STATUS.MIGRATION;
                    this.fifo_frame_window.Dequeue();
                    cursor = p_frame_size;
                    this.migration++;
                    this.fault++;
                }
                else
                {
                    newPage.status = Page.STATUS.PAGEFAULT;
                    cursor++;
                    this.fault++;
                }

                newPage.loc = cursor;
                fifo_frame_window.Enqueue(newPage);
            }
            pageHistory.Add(newPage);
            
            // FIFO 스냅샷 기록
            this.framesHistory.Add(this.fifo_frame_window.Select(p => p.data).ToList());
            
            return newPage.status;
        }

        private Page.STATUS OperateLRU(char data)
        {
            // LRU: 이미 있으면 Hit 처리 후 리스트 끝으로 이동
            int idx = this.frame_window.FindIndex(x => x.data == data);
            Page newPage = new Page();
            if (idx != -1)
            {
                newPage.pid = Page.CREATE_ID++;
                newPage.data = data;
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                // 기존 위치 제거 후 끝에 추가
                this.frame_window.RemoveAt(idx);
                this.frame_window.Add(newPage);
                newPage.loc = this.frame_window.Count;
            }
            else
            {
                newPage.pid = Page.CREATE_ID++;
                newPage.data = data;
                if (frame_window.Count >= p_frame_size)
                {
                    newPage.status = Page.STATUS.MIGRATION;
                    // 가장 오래된 페이지(LRU) 제거
                    this.frame_window.RemoveAt(0);
                    this.migration++;
                    this.fault++;
                }
                else
                {
                    newPage.status = Page.STATUS.PAGEFAULT;
                    this.fault++;
                }
                // 새 페이지를 끝에 추가
                this.frame_window.Add(newPage);
                newPage.loc = this.frame_window.Count;
            }
            this.pageHistory.Add(newPage);
            // LRU 스냅샷 기록
            this.framesHistory.Add(this.frame_window.Select(p => p.data).ToList());
            return newPage.status;
        }        private Page.STATUS OperateLFU(char data)
        {
            Page newPage = new Page();
            newPage.pid = Page.CREATE_ID++;
            newPage.data = data;
            
            // 시간(접근 횟수) 증가
            currentTime++;
            
            // 페이지 참조 빈도 갱신 초기화
            if (!pageFrequency.ContainsKey(data))
            {
                pageFrequency[data] = 0;
            }            // 이미 프레임에 있는 페이지인지 확인
            int idx = this.frame_window.FindIndex(x => x.data == data);
            if (idx != -1)
            {
                // Hit 처리
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                
                // 순수 LFU: 참조 빈도만 증가시키고 위치는 변경하지 않음
                pageFrequency[data]++;
                
                // Hit가 발생한 페이지의 위치를 저장 - 반드시 실제 프레임 내 위치 + 1로 설정해야 함
                newPage.loc = idx + 1;
                
                // pageHistory에 추가할 새 페이지는 현재 위치 정보를 정확히 가져야 함
                // frame_window는 변경하지 않고, pageHistory에만 새 페이지 정보를 추가
            }
            else
            {
                // 페이지 폴트 처리
                if (frame_window.Count >= p_frame_size)
                {
                    // 가장 적게 참조된 페이지 찾기 (빈도 동일시 가장 오래된 페이지)
                    int minFreq = int.MaxValue;
                    int oldestTime = int.MaxValue;
                    int victimIdx = 0;
                    
                    for (int i = 0; i < frame_window.Count; i++)
                    {
                        char pageData = frame_window[i].data;
                        int freq = pageFrequency[pageData];
                        int insertTime = pageInsertionTime[pageData];
                        
                        // 더 낮은 빈도를 가진 페이지 찾기
                        if (freq < minFreq)
                        {
                            minFreq = freq;
                            oldestTime = insertTime;
                            victimIdx = i;
                        }
                        // 빈도가 같은 경우 더 오래된 페이지(먼저 들어온 페이지) 선택
                        else if (freq == minFreq && insertTime < oldestTime)
                        {
                            oldestTime = insertTime;
                            victimIdx = i;
                        }
                    }
                    
                    // 희생 페이지 제거
                    this.frame_window.RemoveAt(victimIdx);
                    newPage.status = Page.STATUS.MIGRATION;
                    this.migration++;
                    this.fault++;
                }
                else
                {
                    newPage.status = Page.STATUS.PAGEFAULT;
                    this.fault++;
                }
                
                // 새 페이지 추가 및 참조 빈도 증가
                this.frame_window.Add(newPage);
                newPage.loc = this.frame_window.Count;
                pageFrequency[data]++;
                
                // 페이지 최초 삽입 시간 기록 (tie-break용)
                pageInsertionTime[data] = currentTime;
            }
            
            this.pageHistory.Add(newPage);
            
            // LFU 스냅샷 기록
            this.framesHistory.Add(this.frame_window.Select(p => p.data).ToList());
            
            return newPage.status;
        }

        public List<Page> GetPageInfo(Page.STATUS status)
        {
            List<Page> pages = new List<Page>();

            foreach (Page page in pageHistory)
            {
                if (page.status == status)
                {
                    pages.Add(page);
                }
            }

            return pages;
        }
    }
}