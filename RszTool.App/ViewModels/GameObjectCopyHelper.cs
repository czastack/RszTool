using RszTool.App.Views;

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

        /// <summary>
        /// Update re4 chainsaw.ContextID
        /// </summary>
        /// <param name="fileOption"></param>
        /// <param name="gameObjectData"></param>
        public static void UpdateContextID(RszFileOption fileOption, IGameObjectData gameObjectData)
        {
            if (fileOption.GameName == GameName.re4)
            {
                var contextIDs = IterGameObjectContextID(gameObjectData);
                UpdateContextIDWindow dialog = new()
                {
                    TreeViewItems = contextIDs
                };
                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// 迭代GameObject以及子物体的组件，查找re4 chainsaw.ContextID
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static IEnumerable<GameObjectContextID> IterGameObjectContextID(IGameObjectData gameObject)
        {
            foreach (var component in gameObject.Components)
            {
                if (component.RSZUserData != null) continue;
                var fields = component.Fields;
                for (int i = 0; i < component.Values.Length; i++)
                {
                    if (fields[i].IsReference && component.Values[i] is RszInstance instanceField)
                    {
                        if (instanceField.RszClass.name == "chainsaw.ContextID")
                        {
                            yield return new GameObjectContextID($"{gameObject}/{component}/{fields[i].name}", instanceField);
                        }
                    }
                }
            }
            foreach (var child in gameObject.GetChildren())
            {
                foreach (var item in IterGameObjectContextID(child))
                {
                    yield return item;
                }
            }
        }
    }


    /// <summary>
    /// For re4 chainsaw.ContextID, when paste game object, update the ContextID
    /// </summary>
    public class GameObjectContextID
    {
        public GameObjectContextID(string name, RszInstance instance)
        {
            Name = name;
            if (instance.RszClass.name != "chainsaw.ContextID")
            {
                throw new ArgumentException("Expected chainsaw.ContextID");
            }
            int groupIndex = instance.RszClass.IndexOfField("_Group");
            int indexIndex = instance.RszClass.IndexOfField("_Index");
            RszFieldNormalViewModel groupViewModel = new(instance, groupIndex);
            RszFieldNormalViewModel indexViewModel = new(instance, indexIndex);
            Items = [
                groupViewModel,
                indexViewModel,
            ];

            groupViewModel.PropertyChanged += (o, e) => {
                App.Instance.SaveData.LastContextID.Group = (int)groupViewModel.Value;
            };
            indexViewModel.PropertyChanged += (o, e) => {
                App.Instance.SaveData.LastContextID.Index = (int)indexViewModel.Value;
            };
        }

        public string Name { get; set; }
        public RszFieldNormalViewModel[] Items { get; set; }
    }
}
