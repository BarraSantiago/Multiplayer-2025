using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class RPCHooker
{
	public static void ApplyHooks()
	{
		Harmony harmony = new Harmony("RPC Hooks");

		//Nota de Lean: No usen Linq en su libreria.
		//Es exesivamente lento y sobrecarga el garbage collector.
		//Esto es solo para un ejemplo.
		List<MethodInfo> rpcMethods = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(assemblys => assemblys.GetTypes())
			.SelectMany(types => types.GetMethods(
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.Static))
			.Where(methods => methods.GetCustomAttribute<RPCAttribute>() != null &&
						methods.ReturnType == typeof(void) &&
						methods.GetParameters().Length == 0)
			.ToList();

		foreach (MethodInfo method in rpcMethods)
		{
			HarmonyMethod patch = new HarmonyMethod(typeof(TargetTestRPC).GetMethod(nameof(TargetTestRPC.Target)));
			harmony.Patch(method, postfix: patch);
		}
	}
}
