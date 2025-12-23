using System;
using HarmonyLib;
using UnityEngine;

public class AttitudeIndicatorMod : IModApi
{
	public void InitMod(Mod mod)
	{
		new Harmony("com.attitudeindicator.mod").PatchAll();
	}
}

[HarmonyPatch(typeof(GameManager), "Awake")]
internal static class GameManagerAwakePatch
{
	private static void Postfix(GameManager __instance)
	{
		AttitudeIndicatorBehaviour.Ensure(__instance);
	}
}

public class AttitudeIndicatorBehaviour : MonoBehaviour
{
	private GUIStyle _labelStyle;
	private const float LabelWidth = 220f;
	private const float LabelHeight = 28f;

	private void Awake()
	{
		_labelStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 18,
			alignment = TextAnchor.UpperCenter,
			normal = { textColor = Color.cyan }
		};
	}

	private void OnGUI()
	{
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}

		var gm = GameManager.Instance;
		var world = gm?.World;
		var player = world?.GetPrimaryPlayer() as EntityPlayerLocal;
		if (player == null)
		{
			return;
		}

		var vehicle = player.AttachedToEntity as EntityVehicle;
		if (!IsGyrocopter(vehicle))
		{
			return;
		}

		float pitch = CalculatePitch(vehicle.transform);
		string label = $"Gyro pitch: {pitch:+0.0;-0.0;0}°";

		var rect = new Rect((Screen.width - LabelWidth) * 0.5f, 60f, LabelWidth, LabelHeight);
		GUI.Label(rect, label, _labelStyle);
	}

	private static bool IsGyrocopter(Entity entity)
	{
		if (entity is not EntityVehicle vehicle)
		{
			return false;
		}

		var name = vehicle.EntityClass?.entityClassName ?? string.Empty;
		return name.IndexOf("gyro", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static float CalculatePitch(Transform transform)
	{
		var forward = transform.forward;
		float clampedY = Mathf.Clamp(forward.y, -1f, 1f);
		return Mathf.Asin(clampedY) * Mathf.Rad2Deg;
	}

	public static void Ensure(GameManager manager)
	{
		if (manager == null)
		{
			return;
		}

		if (manager.gameObject.GetComponent<AttitudeIndicatorBehaviour>() != null)
		{
			return;
		}

		manager.gameObject.AddComponent<AttitudeIndicatorBehaviour>();
	}
}
