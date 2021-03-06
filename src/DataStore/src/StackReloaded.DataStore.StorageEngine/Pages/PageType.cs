﻿namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal enum PageType : byte
    {
        Data = 1,
        Index = 2,
        TextMix = 3,
        TextTree = 4,
        Sort = 7,
        GlobalAllocationMap = 8,
        SharedGlobalAllocationMap = 9,
        IndexAllocationMap = 10,
        PageFreeSpace = 11,
        Boot = 13,
        FileHeader = 15,
        DiffMap = 16,
        MLMap = 17
    }

    //internal enum PageType : byte
    //{
    //    Data = 1,
    //    Index = 2,
    //    TextMix = 3,
    //    TextTree = 4,
    //    Sort = 7,
    //    GlobalAllocationMap = 8,
    //    SharedGlobalAllocationMap = 9,
    //    IAM = 10,
    //    PageFreeSpace = 11,
    //    Boot = 13,
    //    FileHeader = 15,
    //    DiffMap = 16,
    //    MLMap = 17
    //}
}
