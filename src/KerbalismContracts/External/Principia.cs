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

            Utils.Log("Principia found, will use principias history instead of stock orbits");

            return new PrincipiaEvaluator(principia);
		}
	}

    public class PrincipiaEvaluator : IUniverseEvaluator
    {
        private object principia;

        private Vector3d Vector3d(object xyz)
        {
            // Principia does not guarantee that the values are (or will remain to be) fields or properties.
            // "we may change them between fields and properties depending on the phase of the moon or convenience"

            // TODO evaluate the performance impact of this
            double x = Reflection.GetFieldOrPropertyValue<double>(xyz, "x");
            double y = Reflection.GetFieldOrPropertyValue<double>(xyz, "y");
            double z = Reflection.GetFieldOrPropertyValue<double>(xyz, "z");
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
        public static T GetFieldOrPropertyValue<T>(object obj, string name)
        {
            if (obj == null)
            {
                throw new NullReferenceException(
                    $"Cannot access {typeof(T).FullName} {name} on null object");
            }
            Type type = obj.GetType();
            object result = null;
            FieldInfo field = type.GetField(name, public_instance);
            PropertyInfo property = type.GetProperty(name, public_instance);
            if (field != null)
            {
                result = field.GetValue(obj);
            }
            else if (property != null)
            {
                result = property.GetValue(obj, index: null);
            }
            else
            {
                throw new MissingMemberException(
                    $"No public instance field or property {name} in {type.FullName}");
            }
            try
            {
                return (T)result;
            }
            catch (Exception exception)
            {
                throw new InvalidCastException(
                    $@"Could not convert the value of {
                        (field == null ? "property" : "field")} {
                        (field?.FieldType ?? property.PropertyType).FullName} {
                        type.FullName}.{name}, {result}, to {typeof(T).FullName}",
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
