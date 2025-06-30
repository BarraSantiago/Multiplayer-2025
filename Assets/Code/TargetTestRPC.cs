using UnityEngine;

//Nota de Lean: Por limitaciones de Harmony,
//el metodo target tiene que ser estatico.
//Podes agregar parametros como __instance para tener acceso
//a la instacia del objeto que invoco el metodo original
//o __args para los argumentos
//Mas info: https://harmony.pardeike.net/articles/patching-injections.html?utm_source=chatgpt.com
public static class TargetTestRPC
{
	public static void Target()
	{
		Debug.Log("Target");
	}
}
