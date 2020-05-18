using System;
using System.Reflection;
using System.Collections.Generic;

namespace KerbalismContracts
{
    /// <summary>
    /// Principia-specific utilities.
    /// </summary>
    public static class Principia
    {
        public static string AssemblyName()
        {
            foreach (var loaded_assembly in AssemblyLoader.loadedAssemblies)
            {
                if (loaded_assembly.assembly.GetName().Name == "principia.ksp_plugin_adapter")
                {
                    return loaded_assembly.assembly.FullName;
                }
            }
            return null;
        }

        public static Type GetType(string name)
        {
            var assemblyName = AssemblyName();
            if (assemblyName == null)
                return null;
            return Type.GetType($"principia.ksp_plugin_adapter.{name}, {assemblyName}");
        }

        // principia.ksp_plugin_adapter.ExternalInterface.Get().
        public static object Get()
        {
            return GetType("ExternalInterface")?.GetMethod("Get").Invoke(null, null);
        }

		internal static IUniverseEvaluator GetUniverseEvaluator()
		{
            var principia = Get();
            if (principia == null)
                return null;

            Utils.LogDebug("Principia detected, instantiating principias universe evaluator");

            return new PrincipiaEvaluator(principia);
		}
	}

    public class PrincipiaEvaluator : IUniverseEvaluator
    {
        private object principia;

        private Vector3d Vector3d(object xyz)
        {
            double x = Reflection.GetFieldValue<double>(xyz, Reflection.FieldName.x);
            double y = Reflection.GetFieldValue<double>(xyz, Reflection.FieldName.y);
            double z = Reflection.GetFieldValue<double>(xyz, Reflection.FieldName.z);
            return new Vector3d(x, y, z);
        }

        public PrincipiaEvaluator(object principia)
        {
            this.principia = principia;
        }

        public Vector3d GetBodyPosition(CelestialBody body, double time)
        {
            /*
            public XYZ CelestialGetPosition(int body_index, double time)
            {
                ThrowOnError(
                    adapter_.Plugin().ExternalCelestialGetPosition(
                        body_index, time, out XYZ result));
                return result;
            }
			*/

            var xyz = Reflection.Call(principia, "CelestialGetPosition")(body.flightGlobalsIndex, time);
            return Vector3d(xyz);
        }

        public Vector3d GetVesselPosition(Vessel vessel, Vector3d vesselBodyPosition, double time)
        {
            /*
            public XYZ VesselGetPosition(string vessel_guid, double time)
            {
                ThrowOnError(
                    adapter_.Plugin().ExternalVesselGetPosition(
                        vessel_guid, time, out XYZ result));
                return result;
            }
			*/

            var xyz = Reflection.Call(principia, "VesselGetPosition")(vessel.id.ToString(), time);
            return Vector3d(xyz);
        }

        public Vector3d GetWaypointPosition(double latitude, double longitude, CelestialBody body, Vector3d bodyPosition, double time)
        {
            /*
			public XYZ CelestialGetSurfacePosition(
				int body_index,
				double planetocentric_latitude_in_degrees,
				double planetocentric_longitude_in_degrees,
				double radius,
				double time)
			{
				ThrowOnError(
				adapter_.Plugin().ExternalCelestialGetSurfacePosition(
				body_index, planetocentric_latitude_in_degrees,
				planetocentric_longitude_in_degrees, radius, time, out XYZ result));
				return result;
			}
			*/

            var xyz = Reflection.Call(principia, "CelestialGetSurfacePosition")(body.flightGlobalsIndex, latitude, longitude, body.Radius, time);
			return Vector3d(xyz);
        }
    }

	/// <summary>
	/// This class provides the following methods:
	/// — Reflection.Call(obj, "name")(args);
	/// — Reflection.GetFieldOrPropertyValue(obj, "name");
	/// — Reflection.SetFieldOrPropertyValue(obj, "name", value).
	/// The following generics are equivalent to casting the result of the
	/// non-generic versions, with better error messages:
	/// — Reflection.Call<T>(obj, "name")(args) for (T)Reflection.Call(obj, "name")(args);
	/// — Reflection.GetFieldOrPropertyValue<T>(obj, "name") for
	///   (T)Reflection.GetFieldOrPropertyValue(obj, "name").
	/// </summary>
	public static class Reflection
    {
		public enum FieldName
		{
			x, y, z
		}

        private static FieldInfo x;
        private static FieldInfo y;
        private static FieldInfo z;

        public static T GetFieldValue<T>(object obj, FieldName fieldName)
        {
            Type type = obj.GetType();

            FieldInfo f;
			switch (fieldName)
			{
                case FieldName.x:
					if(x == null)
						x = type.GetField("x", public_instance);
                    f = x;
                    break;
                case FieldName.y:
                    if (y == null)
                        y = type.GetField("y", public_instance);
                    f = y;
                    break;
                case FieldName.z:
                    if (z == null)
                        z = type.GetField("z", public_instance);
                    f = z;
                    break;
                default:
					return default(T);
            }

            return (T) f.GetValue(obj);
        }

        public static void SetFieldOrPropertyValue<T>(object obj, string name, T value)
        {
            if (obj == null)
            {
                throw new NullReferenceException(
                    $"Cannot set {typeof(T).FullName} {name} on null object");
            }
            Type type = obj.GetType();
            FieldInfo field = type.GetField(name, public_instance);
            PropertyInfo property = type.GetProperty(name, public_instance);
            if (field == null && property == null)
            {
                throw new MissingMemberException(
                    $"No public instance field or property {name} in {type.FullName}");
            }
            try
            {
                field?.SetValue(obj, value);
                property?.SetValue(obj, value, index: null);
            }
            catch (Exception exception)
            {
                throw new ArgumentException(
                    $@"Could not set {
                        (field == null ? "property" : "field")} {
                        (field?.FieldType ?? property.PropertyType).FullName} {
                        type.FullName}.{name} to {typeof(T).FullName} {
                        value?.GetType().FullName ?? "null"} {value}",
                    exception);
            }
        }
		
        public delegate T BoundMethod<T>(params object[] args);

        public static BoundMethod<T> Call<T>(object obj, string name)
        {
            if (obj == null)
            {
                throw new NullReferenceException($"Cannot call {name} on null object");
            }
            Type type = obj.GetType();
            MethodInfo method = type.GetMethod(name, public_instance);
            if (method == null)
            {
                throw new KeyNotFoundException(
                    $"No public instance method {name} in {type.FullName}");
            }
            return args =>
            {
                object result = method.Invoke(obj, args);
                try
                {
                    return (T)result;
                }
                catch (Exception exception)
                {
                    throw new InvalidCastException(
                        $@"Could not convert the result of {
                            method.ReturnType.FullName} {
                            type.FullName}.{name}(), {result}, to {typeof(T).FullName}",
                        exception);
                }
            };
        }
		
        public static BoundMethod<object> Call(object obj, string name)
        {
            return Call<object>(obj, name);
        }

        private const BindingFlags public_instance =
            BindingFlags.Public | BindingFlags.Instance;
    }
}
