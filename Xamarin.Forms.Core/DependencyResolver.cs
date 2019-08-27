using System;
using System.Linq;
using System.Reflection;

namespace Xamarin.Forms.Internals
{
	public static class DependencyResolver
	{
		static Type _defaultVisualType = typeof(VisualMarker.DefaultVisual);
		static Func<Type, object[], object> Resolver { get; set; }

		public static void ResolveUsing(Func<Type, object[], object> resolver)
		{
			Resolver = resolver;
		}

		public static void ResolveUsing(Func<Type, object> resolver)
		{
			Resolver = (type, objects) => resolver.Invoke(type);
		}

		internal static object Resolve(Type type, params object[] args)
		{
			var result = Resolver?.Invoke(type, args);

			if (result != null)
			{
				if (!type.IsInstanceOfType(result))
				{
					throw new InvalidCastException("Resolved instance is not of the correct type.");
				}
			}

			return result;
		}

		internal static object ResolveOrCreate(Type type) => ResolveOrCreate(type, null, null);

		internal static object ResolveOrCreate(Type type, object source, Type visualType, params object[] args)
		{
			Profile.FrameBegin(type.Name);
			visualType = visualType ?? _defaultVisualType;

			var result = Resolve(type, args);

			if (result == null && args.Length > 0)
			{
				if(visualType != _defaultVisualType)
				{
					if (type.GetTypeInfo().DeclaredConstructors.Any(info => info.GetParameters().Length == 2))
					{
						Profile.FramePartition("Activate 2");
						result = Activator.CreateInstance(type, new[] { args[0], source });
					}
				}

				// This is by no means a general solution to matching with the correct constructor, but it'll
				// do for finding Android renderers which need Context (vs older custom renderers which may still use
				// parameterless constructors)
				if (result == null)
				{
					if (type.GetTypeInfo().DeclaredConstructors.Any(info => info.GetParameters().Length == args.Length))
					{
						Profile.FramePartition("Activate L");
						result = Activator.CreateInstance(type, args);
					}
				}
			}
			
			if (result == null)
			{
				Profile.FramePartition("Activate");
				result = Activator.CreateInstance(type);
			}

			Profile.FrameEnd();

			return result;
		}
	}
}