namespace RszTool
{
    public class RszFileOption
    {
        public GameName GameName { get; set; }
        public int TdbVersion { get; set; }
        public RszParser RszParser { get; set; }

        public RszFileOption(GameName gameName)
        {
            GameName = gameName;
            TdbVersion = gameName switch
            {
                GameName.re4 => 71,
                GameName.re2 => 66,
                GameName.re2rt => 70,
                GameName.re3 => 68,
                GameName.re3rt => 70,
                GameName.re7 => 49,
                GameName.re8 => 69,
                GameName.sf6 => 71,
                _ => 71,
            };
            RszParser = RszParser.GetInstance($"rsz{gameName}.json");
            PostInit();
        }

        public void PostInit()
        {
            if (GameName == GameName.re4)
            {
                var Null = RszParser.GetRSZClass(0);
                if (Null != null)
                {
                    Null.name = "NULL";
                }
                {
                    var rszClass = RszParser.GetRSZClass("via.GameObject");
                    DetectFieldAsFloat(rszClass, "v4");
                }
                {
                    var rszClass = RszParser.GetRSZClass("via.render.Mesh");
                    DetectFieldAsInt(rszClass, "v0", "v10", "v45");
                }
                {
                    var rszClass = RszParser.GetRSZClass("via.physics.Colliders");
                    DetectFieldAsObject(rszClass, "v5");
                }
                {
                    var rszClass = RszParser.GetRSZClass("via.physics.Collider");
                    DetectFieldAsObject(rszClass, "v2", "v3", "v4");
                }
                {
                    var rszClass = RszParser.GetRSZClass("chainsaw.collision.GimmickSensorUserData");
                    DetectFieldAsObject(rszClass, "v1");
                }
                {
                    var rszClass = RszParser.GetRSZClass("chainsaw.collision.GimmickDamageUserData");
                    DetectFieldAsObject(rszClass, "v1");
                }
                {
                    var rszClass = RszParser.GetRSZClass("chainsaw.collision.AttackUserData");
                    DetectFieldAsObject(rszClass, "v1");
                }
                {
                    var rszClass = RszParser.GetRSZClass("chainsaw.collision.DamageUserData");
                    DetectFieldAsObject(rszClass, "v1");
                }
                {
                    var rszClass = RszParser.GetRSZClass("via.motion.Motion");
                    DetectFieldAsObject(rszClass, "v17");
                }
                {
                    var rszClass = RszParser.GetRSZClass("via.navigation.AIMapEffector");
                    DetectFieldAsObject(rszClass, "v3", "v9");
                }
            }
        }

        private static void DetectFieldAs(RszClass? rszClass, RszFieldType type, params string[] fieldNames)
        {
            if (rszClass == null) return;
            foreach (var fieldName in fieldNames)
            {
                if (rszClass?.GetField(fieldName) is RszField field && field.type == RszFieldType.Data && field.size == 4)
                {
                    field.type = type;
                    field.IsTypeInferred = true;
                }
            }
        }

        private static void DetectFieldAsInt(RszClass? rszClass, params string[] fieldNames)
        {
            DetectFieldAs(rszClass, RszFieldType.S32, fieldNames);
        }

        private static void DetectFieldAsObject(RszClass? rszClass, params string[] fieldNames)
        {
            DetectFieldAs(rszClass, RszFieldType.Object, fieldNames);
        }

        private static void DetectFieldAsFloat(RszClass? rszClass, params string[] fieldNames)
        {
            DetectFieldAs(rszClass, RszFieldType.F32, fieldNames);
        }
    }
}
