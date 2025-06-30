using UnityEngine;

public class TestRPC : MonoBehaviour
{
	void Awake()
	{
		RPCHooker.ApplyHooks();
	}

	void Start()
	{
		Original();
	}

	[RPC]
	public void Original()
	{
		Debug.Log("Original");
	}	
}
