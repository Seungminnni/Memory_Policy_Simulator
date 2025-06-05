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
        private int cursor;            // 현재 프레임 위치

        public Core(int get_frame_size, POLICY policy = POLICY.FIFO)
        {
            this.p_frame_size = get_frame_size;
            this.policy = policy;
            this.pageHistory = new List<Page>();
            this.frame_window = new Queue<Page>();
            this.frequency = new Dictionary<char, int>();
            this.hit = 0;
            this.fault = 0;
            this.migration = 0;
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
                    }
                    else if (policy == POLICY.MFU)
                    {
                        // 참조 횟수 증가
                        if (!frequency.ContainsKey(data))
                            frequency[data] = 0;
                        frequency[data]++;

                        // 참조 횟수가 적은 순서대로 정렬
                        temp.Sort((a, b) =>
                        {
                            int freqA = frequency.ContainsKey(a.data) ? frequency[a.data] : 0;
                            int freqB = frequency.ContainsKey(b.data) ? frequency[b.data] : 0;
                            return freqA.CompareTo(freqB);
                        });

                        // 정렬된 순서대로 다시 큐에 넣음
                        foreach (var page in temp)
                            frame_window.Enqueue(page);
                        frame_window.Enqueue(hitPage);
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
                        // 가장 많이 참조된 페이지 찾기
                        var temp = new List<Page>();
                        Page maxFreqPage = frame_window.Peek();
                        int maxFreq = frequency.ContainsKey(maxFreqPage.data) ? frequency[maxFreqPage.data] : 0;

                        foreach (var page in frame_window)
                        {
                            int freq = frequency.ContainsKey(page.data) ? frequency[page.data] : 0;
                            if (freq > maxFreq)
                            {
                                maxFreq = freq;
                                maxFreqPage = page;
                            }
                            temp.Add(page);
                        }

                        frame_window.Clear();
                        foreach (var page in temp)
                        {
                            if (page.data != maxFreqPage.data)
                                frame_window.Enqueue(page);
                        }

                        if (frequency.ContainsKey(maxFreqPage.data))
                            frequency.Remove(maxFreqPage.data);
                    }
                    else  // FIFO 또는 LRU
                    {
                        var oldPage = frame_window.Dequeue();
                        if (frequency.ContainsKey(oldPage.data))
                            frequency.Remove(oldPage.data);
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
                    frequency[data] = 1;  // 새 페이지의 참조 횟수 초기화

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