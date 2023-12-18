namespace RszTool.App.ViewModels
{
    public static class GameObjectCopyHelper
    {
        private static PfbFile.GameObjectData? CopiedPfbGameObject { get; set; }
        private static ScnFile.GameObjectData? CopiedScnGameObject { get; set; }

        public static void CopyGameObject(PfbFile.GameObjectData gameObject)
        {
            CopiedPfbGameObject = gameObject;
            CopiedScnGameObject = null;
        }

        public static void CopyGameObject(ScnFile.GameObjectData gameObject)
        {
            CopiedScnGameObject = gameObject;
            CopiedPfbGameObject = null;
        }

        public static PfbFile.GameObjectData? GetCopiedPfbGameObject()
        {
            if (CopiedPfbGameObject != null) return CopiedPfbGameObject;
            if (CopiedScnGameObject != null)
                return PfbFile.GameObjectData.FromScnGameObject(CopiedScnGameObject);
            return null;
        }

        public static ScnFile.GameObjectData? GetCopiedScnGameObject()
        {
            if (CopiedScnGameObject != null) return CopiedScnGameObject;
            if (CopiedPfbGameObject != null)
                return ScnFile.GameObjectData.FromPfbGameObject(CopiedPfbGameObject);
            return null;
        }
    }
}
