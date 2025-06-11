# Memory Policy Simulator

A Windows Forms application that simulates page replacement algorithms such as FIFO, LRU, MFU, and a hybrid policy labeled **NEW**.

## Build Requirements

- Visual Studio 2019 or later
- .NET Framework 4.8

## Usage

1. Open `Memory_Policy_Simulator.sln` in Visual Studio.
2. Choose a page replacement policy from the dropdown.
3. Enter a reference string and number of frames.
4. Optionally set the phase window (W) and threshold (T) when using the **NEW** policy.
5. Click **Run** to view page faults and hits visualized.

This simulator was originally designed for basic policies and can be extended with additional algorithms.
