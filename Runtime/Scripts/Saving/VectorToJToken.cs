using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public static class VectorToJToken
    {
        public static JObject CaptureVector(Vector2 vector2)
        {
            JObject result = new JObject();

            result["x"] = vector2.x;
            result["y"] = vector2.y;

            return result;
        }

        public static JObject CaptureVector(Vector3 vector3)
        {
            JObject result = new JObject();

            result["x"] = vector3.x;
            result["y"] = vector3.y;
            result["z"] = vector3.z;
            return result;
        }

        public static JObject CaptureVector(Vector4 vector)
        {
            JObject result = new JObject();

            result["x"] = vector.x;
            result["y"] = vector.y;
            result["z"] = vector.z;
            result["w"] = vector.w;

            return result;
        }
    }
}
