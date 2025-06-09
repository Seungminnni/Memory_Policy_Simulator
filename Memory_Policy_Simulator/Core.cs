using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_Policy_Simulator
{
    public class Core
    {
        public enum POLICY { FIFO, LRU, MFU }
        public POLICY policy;
        public int p_frame_size;       // 프레임의 크기
        public int hit;                // 페이지 히트 횟수
        public int fault;              // 페이지 폴트 횟수
        public int migration;          // 페이지 교체 횟수
        public List<Page> pageHistory; // 전체 페이지 접근 기록

        private Queue<Page> frame_window;      // 프레임 윈도우
        private Dictionary<char, int> frequency;  // MFU용 페이지 참조 횟수
        private Dictionary<char, int> insertionOrder;  // MFU용 페이지 최초 삽입 시점
        private int insertionCount;  // 삽입 시점 카운터
        private int cursor;            // 현재 프레임 위치

        public Core(int get_frame_size, POLICY policy = POLICY.FIFO)
        {
            this.p_frame_size = get_frame_size;
            this.policy = policy;
            this.pageHistory = new List<Page>();
            this.frame_window = new Queue<Page>();
            this.frequency = new Dictionary<char, int>();
            this.insertionOrder = new Dictionary<char, int>();
            this.insertionCount = 0;
            this.cursor = 0;
        }

        public Page.STATUS Operate(char data)
        {
            Page newPage = new Page();

            if (this.frame_window.Any(x => x.data == data))
            {
                newPage.pid = Page.CREATE_ID++;
                newPage.data = data;
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                int i;

                for (i = 0; i < this.frame_window.Count; i++)
                {
                    if (this.frame_window.ElementAt(i).data == data) break;
                }
                newPage.loc = i + 1;

                // LRU나 MFU일 때는 hit된 페이지 재배치
                if (policy != POLICY.FIFO)
                {
                    var temp = new List<Page>();
                    var hitPage = this.frame_window.ElementAt(i);

                    // 현재 프레임의 모든 페이지를 리스트로 복사
                    foreach (var page in frame_window)
                    {
                        if (page.data != data)
                            temp.Add(page);
                    }

                    frame_window.Clear();

                    if (policy == POLICY.LRU)
                    {
                        // 모든 페이지를 다시 큐에 넣고, hit된 페이지는 마지막에 넣음
                        foreach (var page in temp)
                            frame_window.Enqueue(page);
                        frame_window.Enqueue(hitPage);
                    }                    else if (policy == POLICY.MFU)
                    {
                        // 참조 횟수만 증가시키고, 순서는 유지
                        if (!frequency.ContainsKey(data))
                        {
                            frequency[data] = 0;
                            insertionOrder[data] = insertionCount++;
                        }
                        frequency[data]++;

                        // MFU에서는 HIT 시 페이지 순서를 변경하지 않음
                        // 원래 위치에 그대로 다시 넣기
                        for (int k = 0; k < temp.Count + 1; k++)
                        {
                            if (k == i)
                                frame_window.Enqueue(hitPage);
                            else if (k < i)
                                frame_window.Enqueue(temp[k]);
                            else
                                frame_window.Enqueue(temp[k - 1]);
                        }
                    }
                }
            }
            else
            {
                newPage.pid = Page.CREATE_ID++;
                newPage.data = data;

                if (frame_window.Count >= p_frame_size)
                {
                    newPage.status = Page.STATUS.MIGRATION;

                    if (policy == POLICY.MFU)
                    {
                        // 가장 많이 참조된 페이지와 그 중에서 가장 나중에 들어온 페이지 찾기
                        var temp = new List<Page>();
                        Page victimPage = frame_window.Peek();
                        int maxFreq = frequency.ContainsKey(victimPage.data) ? frequency[victimPage.data] : 0;
                        int latestInsertion = insertionOrder.ContainsKey(victimPage.data) ? insertionOrder[victimPage.data] : 0;

                        foreach (var page in frame_window)
                        {
                            int freq = frequency.ContainsKey(page.data) ? frequency[page.data] : 0;
                            int insertion = insertionOrder.ContainsKey(page.data) ? insertionOrder[page.data] : 0;

                            // 1. 빈도수가 더 높은 페이지를 선택
                            // 2. 빈도수가 같으면 나중에 들어온 페이지를 선택
                            if (freq > maxFreq || (freq == maxFreq && insertion > latestInsertion))
                            {
                                maxFreq = freq;
                                latestInsertion = insertion;
                                victimPage = page;
                            }
                            temp.Add(page);
                        }

                        frame_window.Clear();
                        foreach (var page in temp)
                        {
                            if (page.data != victimPage.data)
                                frame_window.Enqueue(page);
                        }

                        if (frequency.ContainsKey(victimPage.data))
                        {
                            frequency.Remove(victimPage.data);
                            insertionOrder.Remove(victimPage.data);
                        }
                    }
                    else  // FIFO 또는 LRU
                    {
                        var oldPage = frame_window.Dequeue();
                        if (frequency.ContainsKey(oldPage.data))
                        {
                            frequency.Remove(oldPage.data);
                            insertionOrder.Remove(oldPage.data);
                        }
                    }

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

                if (policy == POLICY.MFU)
                {
                    frequency[data] = 1;  // 새 페이지의 참조 횟수 초기화
                    insertionOrder[data] = insertionCount++;  // 삽입 시점 기록
                }

                newPage.loc = cursor;
                frame_window.Enqueue(newPage);
            }
            pageHistory.Add(newPage);

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