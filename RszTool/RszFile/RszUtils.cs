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

        public static void SyncResourceFromRsz(List<ResourceInfo> resourcesInfos, RSZFile rsz)
        {
            resourcesInfos.Clear();
            HashSet<string> addedPath = new();

            void CheckResouce(string path)
            {
                if (path.Contains('/') && !addedPath.Contains(path))
                {
                    addedPath.Add(path);
                    resourcesInfos.Add(new ResourceInfo { resourcePath = path });
                }
            }

            foreach (var instance in rsz.InstanceList)
            {
                if (instance.RSZUserData != null) continue;
                for (int i = 0; i < instance.RszClass.fields.Length; i++)
                {
                    var field = instance.RszClass.fields[i];
                    if (field.type == RszFieldType.String || field.type == RszFieldType.Resource)
                    {
                        if (field.array)
                        {
                            foreach (var item in (List<object>)instance.Values[i])
                            {
                                CheckResouce((string)item);
                            }
                        }
                        else
                        {
                            CheckResouce((string)instance.Values[i]);
                        }
                    }
                }
            }
        }
    }
}
