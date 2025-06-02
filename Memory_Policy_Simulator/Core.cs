using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_Policy_Simulator
{
    class Core
    {
        private int cursor;
        public int p_frame_size;
        public List<Page> frame_window;
        public Queue<Page> fifo_frame_window;
        public List<Page> pageHistory;
        // 각 Operate 후 프레임 상태를 저장 (시각화 용)
        public List<List<char>> framesHistory;

        public int hit;
        public int fault;
        public int migration;
        
        // 페이지 교체 정책
        public enum POLICY { FIFO, LRU }
        public POLICY policy;

        public Core(int get_frame_size, POLICY policy = POLICY.LRU)
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
            else // LRU
            {
                this.frame_window = new List<Page>();
            }
        }

        public Page.STATUS Operate(char data)
        {
            if (policy == POLICY.FIFO)
                return OperateFIFO(data);
            else
                return OperateLRU(data);
        }

        private Page.STATUS OperateFIFO(char data)
        {
            Page newPage;

            if (this.fifo_frame_window.Any<Page>(x => x.data == data))
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