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
        public int p_frame_size;
        public int hit;
        public int fault;
        public int migration;
        public List<Page> pageHistory;

        // FIFO
        private Queue<Page> fifo_window;
        // LRU
        private List<Page> lru_window;
        // MFU
        private List<Page> mfu_window;
        private Dictionary<char, int> mfu_freq;

        public Core(int get_frame_size, POLICY policy = POLICY.FIFO)
        {
            this.p_frame_size = get_frame_size;
            this.policy = policy;
            this.pageHistory = new List<Page>();
            this.hit = 0;
            this.fault = 0;
            this.migration = 0;
            if (policy == POLICY.FIFO)
                this.fifo_window = new Queue<Page>();
            if (policy == POLICY.LRU)
                this.lru_window = new List<Page>();
            if (policy == POLICY.MFU) {
                this.mfu_window = new List<Page>();
                this.mfu_freq = new Dictionary<char, int>();
            }
        }

        public Page.STATUS Operate(char data)
        {
            switch (policy)
            {
                case POLICY.FIFO: return OperateFIFO(data);
                case POLICY.LRU:  return OperateLRU(data);
                case POLICY.MFU:  return OperateMFU(data);
                default: return OperateFIFO(data);
            }
        }

        // FIFO
        private Page.STATUS OperateFIFO(char data)
        {
            Page newPage = new Page();
            newPage.pid = Page.CREATE_ID++;
            newPage.data = data;
            if (this.fifo_window.Any(x => x.data == data))
            {
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                int i = 0;
                foreach (var p in this.fifo_window) { if (p.data == data) break; i++; }
                newPage.loc = i + 1;
            }
            else
            {
                if (fifo_window.Count >= p_frame_size)
                {
                    newPage.status = Page.STATUS.MIGRATION;
                    this.fifo_window.Dequeue();
                    this.migration++;
                    this.fault++;
                }
                else
                {
                    newPage.status = Page.STATUS.PAGEFAULT;
                    this.fault++;
                }
                newPage.loc = Math.Min(fifo_window.Count + 1, p_frame_size);
                fifo_window.Enqueue(newPage);
            }
            pageHistory.Add(newPage);
            return newPage.status;
        }

        // LRU        
        private Page.STATUS OperateLRU(char data)
        {
            Page newPage = new Page();
            newPage.pid = Page.CREATE_ID++;
            newPage.data = data;
            int idx = lru_window.FindIndex(x => x.data == data);
            if (idx != -1)
            {
                // Hit: mark the page as most recently used
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                
                // Remove existing page and move to back (most recent)
                lru_window.RemoveAt(idx);
                lru_window.Add(newPage);
                newPage.loc = idx + 1;  // maintain original position for visualization
            }
            else
            {
                if (lru_window.Count >= p_frame_size)
                {
                    // Miss & Migration: remove least recently used page (front)
                    newPage.status = Page.STATUS.MIGRATION;
                    lru_window.RemoveAt(0);
                    this.migration++;
                    this.fault++;
                }
                else
                {
                    // Miss & PageFault: add new page
                    newPage.status = Page.STATUS.PAGEFAULT;
                    this.fault++;
                }
                // Add new page to back as most recently used
                lru_window.Add(newPage);
                newPage.loc = lru_window.Count;
            }
            pageHistory.Add(newPage);
            return newPage.status;
        }

        // MFU
        private Page.STATUS OperateMFU(char data)
        {
            Page newPage = new Page();
            newPage.pid = Page.CREATE_ID++;
            newPage.data = data;

            // Initialize frequency if not exists
            if (!mfu_freq.ContainsKey(data))
            {
                mfu_freq[data] = 0;
            }

            int idx = mfu_window.FindIndex(x => x.data == data);
            if (idx != -1)
            {
                // Hit: increment frequency and maintain position
                newPage.status = Page.STATUS.HIT;
                this.hit++;
                mfu_freq[data]++;
                newPage.loc = idx + 1;
                mfu_window[idx] = newPage; // Update the page in place
            }
            else
            {
                if (mfu_window.Count >= p_frame_size)
                {
                    // Find highest frequency
                    int maxFreq = mfu_window.Max(p => mfu_freq[p.data]);
                    
                    // Find the oldest page among those with max frequency
                    var victimPage = mfu_window
                        .Where(p => mfu_freq[p.data] == maxFreq)
                        .OrderBy(p => p.pid)
                        .First();

                    int victimIdx = mfu_window.IndexOf(victimPage);
                    mfu_freq.Remove(victimPage.data);
                    mfu_window.RemoveAt(victimIdx);

                    newPage.status = Page.STATUS.MIGRATION;
                    this.migration++;
                    this.fault++;
                }
                else
                {
                    newPage.status = Page.STATUS.PAGEFAULT;
                    this.fault++;
                }
                
                // Add new page
                mfu_window.Add(newPage);
                mfu_freq[data] = 1; // Initialize frequency to 1
                newPage.loc = mfu_window.Count;
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