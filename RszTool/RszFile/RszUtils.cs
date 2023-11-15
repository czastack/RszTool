namespace RszTool
{
    public static class RszUtils
    {
        public static void AddUserDataFromRsz(List<UserdataInfo> userdataInfos, RSZFile rsz, int userDataStart = 0, int length = -1)
        {
            if (length == -1) length = rsz.RSZUserDataInfoList.Count - userDataStart;
            for (int i = 0; i < length; i++)
            {
                var item = rsz.RSZUserDataInfoList[i + userDataStart];
                if (item is RSZUserDataInfo info)
                {
                    userdataInfos.Add(info.ToUserdataInfo(rsz.RszParser));
                }
            }
        }

        public static void SyncUserDataFromRsz(List<UserdataInfo> userdataInfos, RSZFile rsz)
        {
            userdataInfos.Clear();
            AddUserDataFromRsz(userdataInfos, rsz);
        }

        public static void AddResourceFromRsz(List<ResourceInfo> resourcesInfos, RSZFile rsz, int instanceStart = 0, int length = -1)
        {
            if (length == -1) length = rsz.InstanceList.Count - instanceStart;
            HashSet<string> addedPath = new();
            void CheckResouce(string path)
            {
                if (path.Contains('/') && !addedPath.Contains(path))
                {
                    addedPath.Add(path);
                    resourcesInfos.Add(new ResourceInfo { resourcePath = path });
                }
            }

            for (int i = 0; i < length; i++)
            {
                var instance = rsz.InstanceList[i + instanceStart];
                if (instance.RSZUserData != null) continue;
                for (int j = 0; j < instance.RszClass.fields.Length; j++)
                {
                    var field = instance.RszClass.fields[j];
                    if (field.type == RszFieldType.String || field.type == RszFieldType.Resource)
                    {
                        if (field.array)
                        {
                            foreach (var item in (List<object>)instance.Values[j])
                            {
                                CheckResouce((string)item);
                            }
                        }
                        else
                        {
                            CheckResouce((string)instance.Values[j]);
                        }
                    }
                }
            }
        }

        public static void SyncResourceFromRsz(List<ResourceInfo> resourcesInfos, RSZFile rsz)
        {
            resourcesInfos.Clear();
            AddResourceFromRsz(resourcesInfos, rsz);
        }
    }
}
