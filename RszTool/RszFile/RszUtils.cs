namespace RszTool
{
    public static class RszUtils
    {
        public static void SyncUserDataFromRsz(List<UserdataInfo> userdataInfos, RSZFile rsz)
        {
            userdataInfos.Clear();
            foreach (var item in rsz.RSZUserDataInfoList)
            {
                if (item is RSZUserDataInfo info)
                {
                    userdataInfos.Add(info.ToUserdataInfo(rsz.RszParser));
                }
            }
        }
    }
}
