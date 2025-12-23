using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

public class AttitudeIndicatorMod : IModApi
{
	public void InitMod(Mod mod)
	{
		UnityEngine.Debug.Log("[AttitudeIndicator] InitMod starting");
		new Harmony("com.attitudeindicator.mod").PatchAll();
		AttitudeIndicatorBehaviour.EnsureSingleton();
		UnityEngine.Debug.Log("[AttitudeIndicator] Harmony patches applied and behaviour ensured");
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
	private static AttitudeIndicatorBehaviour _instance;
	private GUIStyle _labelStyle;
	private const float LabelWidth = 220f;
	private const float LabelHeight = 28f;

	private readonly float _gaugeWidth = 26f;
	private readonly float _gaugeHeight = 160f;
	private readonly float _gaugeRightPadding = 105f;
	private readonly float _gaugeBottomPadding = 170f;
	private bool _wasInGyro;

	public static readonly AttitudeUiConfig Config = new AttitudeUiConfig();

	public static void EnsureSingleton()
	{
		if (_instance != null)
		{
			return;
		}

		var go = new GameObject("AttitudeIndicatorBehaviour");
		DontDestroyOnLoad(go);
		_instance = go.AddComponent<AttitudeIndicatorBehaviour>();
		UnityEngine.Debug.Log("[AttitudeIndicator] Behaviour singleton created");

		// Load config on startup
		AttitudeUiConfig.LoadFromFile();
	}

	private void Awake()
	{
		UnityEngine.Debug.Log("[AttitudeIndicator] Behaviour Awake");
	}

	private void OnGUI()
	{
		EnsureStyle();

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
			if (_wasInGyro)
			{
				UnityEngine.Debug.Log("[AttitudeIndicator] Player left gyrocopter");
				_wasInGyro = false;
			}
			return;
		}

		if (!_wasInGyro)
		{
			UnityEngine.Debug.Log("[AttitudeIndicator] Player entered gyrocopter; attitude display active");
			_wasInGyro = true;
		}

		float pitch = CalculatePitch(vehicle.transform);
		string label = $"Gyro pitch: {pitch:+0.0;-0.0;0}°";

		DrawAttitudeGauge(pitch, label);
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

	private void DrawAttitudeGauge(float pitchDegrees, string label)
	{
		float normalized = Mathf.Clamp(pitchDegrees / 45f, -1f, 1f);
		float scale = Mathf.Max(0.05f, Config.Scale);

		float width = _gaugeWidth * scale;
		float height = _gaugeHeight * scale;
		float x = Screen.width - _gaugeRightPadding + Config.OffsetX;
		float y = Screen.height - _gaugeBottomPadding - height + Config.OffsetY;
		var gaugeRect = new Rect(x, y, width, height);

		// Background fill
		var prevColor = GUI.color;
		GUI.color = Config.BgColor;
		GUI.DrawTexture(gaugeRect, Texture2D.whiteTexture);

		// Border
		GUI.color = Config.BaseColor;
		GUI.DrawTexture(new Rect(gaugeRect.x, gaugeRect.y, gaugeRect.width, 2f), Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(gaugeRect.x, gaugeRect.yMax - 2f, gaugeRect.width, 2f), Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(gaugeRect.x, gaugeRect.y, 2f, gaugeRect.height), Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(gaugeRect.xMax - 2f, gaugeRect.y, 2f, gaugeRect.height), Texture2D.whiteTexture);

		// Horizon (level) line
		float centerY = gaugeRect.y + gaugeRect.height * 0.5f;
		GUI.DrawTexture(new Rect(gaugeRect.x + 2f, centerY - 1f, gaugeRect.width - 4f, 2f), Texture2D.whiteTexture);

		// Pitch indicator
		float travel = (gaugeRect.height * 0.5f) - 4f;
		float offset = -normalized * travel;
		float indicatorY = centerY + offset - 2f;
		GUI.color = Config.LevelColor;
		GUI.DrawTexture(new Rect(gaugeRect.x + 4f, indicatorY, gaugeRect.width - 8f, 4f), Texture2D.whiteTexture);

		if (Config.ShowText)
		{
			float textX = gaugeRect.x - ((LabelWidth - gaugeRect.width) * 0.5f);
			float textY = gaugeRect.y - LabelHeight - 6f;
			var labelRect = new Rect(textX, textY, LabelWidth, LabelHeight);
			GUI.color = Config.FontColor;
			_labelStyle.normal.textColor = Config.FontColor;
			_labelStyle.fontSize = Config.FontSize;
			GUI.Label(labelRect, label, _labelStyle);
		}

		GUI.color = prevColor;
	}

	private void EnsureStyle()
	{
		if (_labelStyle == null)
		{
			_labelStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = Config.FontSize,
				normal = { textColor = Config.FontColor }
			};
			UnityEngine.Debug.Log("[AttitudeIndicator] GUI style initialized in OnGUI");
		}
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

	public static void NotifyConfigChanged()
	{
		if (_instance != null)
		{
			_instance._labelStyle = null;
		}
		try {
			AttitudeUiConfig.SaveToFile();
			UnityEngine.Debug.Log("[AttitudeIndicator] Config saved to file.");
		} catch (Exception ex) {
			UnityEngine.Debug.Log($"[AttitudeIndicator] Config save failed: {ex.Message}");
		}
	}
}

public class AttitudeUiConfig
{
	public float Scale = 1f;
	public float OffsetX = 0f;
	public float OffsetY = 0f;
	public Color BgColor = new Color(0f, 0f, 0f, 0.55f);
	// ...existing code...

	public static void SaveToFile()
	{
		// Fix: Use correct Mods folder and ensure directory exists
		var baseDir = System.IO.Directory.GetCurrentDirectory();
		var configDir = System.IO.Path.Combine(baseDir, "Mods", "AttitudeIndicator");
		var path = System.IO.Path.Combine(configDir, "config.xml");
		if (!System.IO.Directory.Exists(configDir))
		{
			System.IO.Directory.CreateDirectory(configDir);
		}
		var doc = new System.Xml.Linq.XDocument(
			new System.Xml.Linq.XElement("AttitudeIndicatorConfig",
				new System.Xml.Linq.XElement("Scale", new System.Xml.Linq.XAttribute("value", AttitudeIndicatorBehaviour.Config.Scale.ToString(CultureInfo.InvariantCulture))),
				new System.Xml.Linq.XElement("OffsetX", new System.Xml.Linq.XAttribute("value", AttitudeIndicatorBehaviour.Config.OffsetX.ToString(CultureInfo.InvariantCulture))),
				new System.Xml.Linq.XElement("OffsetY", new System.Xml.Linq.XAttribute("value", AttitudeIndicatorBehaviour.Config.OffsetY.ToString(CultureInfo.InvariantCulture))),
				new System.Xml.Linq.XElement("BgColor", new System.Xml.Linq.XAttribute("value", ColorToHex(AttitudeIndicatorBehaviour.Config.BgColor))),
				new System.Xml.Linq.XElement("BaseColor", new System.Xml.Linq.XAttribute("value", ColorToHex(AttitudeIndicatorBehaviour.Config.BaseColor))),
				new System.Xml.Linq.XElement("LevelColor", new System.Xml.Linq.XAttribute("value", ColorToHex(AttitudeIndicatorBehaviour.Config.LevelColor))),
				new System.Xml.Linq.XElement("FontColor", new System.Xml.Linq.XAttribute("value", ColorToHex(AttitudeIndicatorBehaviour.Config.FontColor))),
				new System.Xml.Linq.XElement("FontSize", new System.Xml.Linq.XAttribute("value", AttitudeIndicatorBehaviour.Config.FontSize.ToString(CultureInfo.InvariantCulture))),
				new System.Xml.Linq.XElement("ShowText", new System.Xml.Linq.XAttribute("value", AttitudeIndicatorBehaviour.Config.ShowText ? "true" : "false"))
			)
		);
		doc.Save(path);
	}

	public static bool TryParseHexColor(string input, out Color color)
	{
		color = Color.white;
		if (string.IsNullOrEmpty(input))
		{
			return false;
		}
		string hex = input.Trim();
		if (hex.StartsWith("#"))
		{
			hex = hex.Substring(1);
		}
		if (hex.Length != 6 && hex.Length != 8)
		{
			return false;
		}
		try
		{
			byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			byte a = 255;
			if (hex.Length == 8)
			{
				a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			}
			color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
			return true;
		}
		catch
		{
			return false;
		}
	}


	private static string ColorToHex(Color c)
	{
		int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
		int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
		int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
			// AttitudeIndicator main mod logic (tools directory fully removed)
		return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
	}
	public Color BaseColor = new Color(0.1f, 0.9f, 0.1f, 0.85f);
	public Color LevelColor = new Color(0.2f, 0.6f, 1f, 0.9f);
	public Color FontColor = Color.cyan;
	public int FontSize = 18;
	public bool ShowText = true;

	private static readonly string ConfigPath = "Mod/AttitudeIndicator/config.xml";

	public static void LoadFromFile()
	{
		try
		{
			var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ConfigPath);
			if (!System.IO.File.Exists(path)) return;
			var xml = System.Xml.Linq.XDocument.Load(path);
			var root = xml.Element("AttitudeIndicatorConfig");
			if (root == null) return;

			foreach (var el in root.Elements())
			{
				var val = el.Attribute("value")?.Value;
				if (string.IsNullOrEmpty(val)) continue;
				switch (el.Name.LocalName.ToLowerInvariant())
				{
					case "scale":
						if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
							AttitudeIndicatorBehaviour.Config.Scale = scale;
						break;
					case "offsetx":
						if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float ox))
							AttitudeIndicatorBehaviour.Config.OffsetX = ox;
						break;
					case "offsety":
						if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float oy))
							AttitudeIndicatorBehaviour.Config.OffsetY = oy;
						break;
					case "bgcolor":
						if (TryParseHexColor(val, out Color bg))
							AttitudeIndicatorBehaviour.Config.BgColor = bg;
						break;
					case "basecolor":
						if (TryParseHexColor(val, out Color bc))
							AttitudeIndicatorBehaviour.Config.BaseColor = bc;
						break;
					case "levelcolor":
						if (TryParseHexColor(val, out Color lc))
							AttitudeIndicatorBehaviour.Config.LevelColor = lc;
						break;
					case "fontcolor":
						if (TryParseHexColor(val, out Color fc))
							AttitudeIndicatorBehaviour.Config.FontColor = fc;
						break;
					case "fontsize":
						if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int fs))
							AttitudeIndicatorBehaviour.Config.FontSize = fs;
						break;
					case "showtext":
						AttitudeIndicatorBehaviour.Config.ShowText = val.ToLowerInvariant() == "true" || val == "1";
						break;
				}
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log($"[AttitudeIndicator] Config load error: {ex.Message}");
		}
	}
}

public class ConsoleCmdAttitudeUi : ConsoleCmdAbstract
{
		public override string getDescription()
	{
		return "Adjust Attitude Indicator UI settings";
	}

		public override string getHelp()
	{
			return "Usage: attui [scale <f>] [movex <f>] [movey <f>] [color base|level|font|bg r g b (a)|#RRGGBB(AA)] [font size <int>] [text on|off] [reset] [show]";
	}

		public override string[] getCommands()
	{
		return new[] { "attui", "attitudeui" };
	}

	public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
	{
		if (_params.Count == 0)
		{
			OutputCurrent();
			return;
		}

		string action = _params[0].ToLowerInvariant();
		var cfg = AttitudeIndicatorBehaviour.Config;

		switch (action)
		{
			case "scale":
				if (_params.Count >= 2 && TryParseFloat(_params[1], out float scale))
				{
					cfg.Scale = Mathf.Max(0.1f, scale);
					AfterChange("scale", cfg.Scale.ToString("0.###", CultureInfo.InvariantCulture));
				}
				else
				{
					Output("attui scale <float>");
				}
				break;
			case "movex":
			case "offsetx":
				if (_params.Count >= 2 && TryParseFloat(_params[1], out float ox))
				{
					cfg.OffsetX = ox;
					AfterChange("offsetX", cfg.OffsetX.ToString("0.##", CultureInfo.InvariantCulture));
				}
				else
				{
					Output("attui movex <float>");
				}
				break;
			case "movey":
			case "offsety":
				if (_params.Count >= 2 && TryParseFloat(_params[1], out float oy))
				{
					cfg.OffsetY = oy;
					AfterChange("offsetY", cfg.OffsetY.ToString("0.##", CultureInfo.InvariantCulture));
				}
				else
				{
					Output("attui movey <float>");
				}
				break;
			case "color":
				if (_params.Count >= 3)
				{
					string target = _params[1].ToLowerInvariant();
					if (TryParseColor(_params, 2, out Color color))
					{
						switch (target)
						{
							case "base":
								cfg.BaseColor = color;
								AfterChange("base color", ColorToString(color));
								break;
							case "level":
								cfg.LevelColor = color;
								AfterChange("level color", ColorToString(color));
								break;
							case "font":
								cfg.FontColor = color;
								AfterChange("font color", ColorToString(color));
								break;
							case "bg":
							case "background":
								cfg.BgColor = color;
								AfterChange("background color", ColorToString(color));
								break;
							default:
								Output("color target must be base|level|font|bg");
								break;
						}
					}
				}
				else
				{
					Output("attui color base|level|font|bg r g b [a] | attui color base|level|font|bg #RRGGBB[AA]");
				}
				break;
			case "font":
				if (_params.Count >= 3 && _params[1].ToLowerInvariant() == "size" && int.TryParse(_params[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fs))
				{
					cfg.FontSize = Mathf.Max(10, fs);
					AfterChange("font size", cfg.FontSize.ToString(CultureInfo.InvariantCulture));
				}
				else
				{
					Output("attui font size <int>");
				}
				break;
			case "text":
				if (_params.Count >= 2)
				{
					string val = _params[1].ToLowerInvariant();
					cfg.ShowText = val == "on" || val == "1" || val == "true";
					AfterChange("text", cfg.ShowText ? "on" : "off");
				}
				else
				{
					Output("attui text on|off");
				}
				break;
			case "reset":
				AttitudeIndicatorBehaviour.Config.Scale = 1f;
				AttitudeIndicatorBehaviour.Config.OffsetX = 0f;
				AttitudeIndicatorBehaviour.Config.OffsetY = 0f;
				AttitudeIndicatorBehaviour.Config.BgColor = new Color(0f, 0f, 0f, 0.55f);
				AttitudeIndicatorBehaviour.Config.BaseColor = new Color(0.1f, 0.9f, 0.1f, 0.85f);
				AttitudeIndicatorBehaviour.Config.LevelColor = new Color(0.2f, 0.6f, 1f, 0.9f);
				AttitudeIndicatorBehaviour.Config.FontColor = Color.cyan;
				AttitudeIndicatorBehaviour.Config.FontSize = 18;
				AttitudeIndicatorBehaviour.Config.ShowText = true;
				AfterChange("reset", "defaults restored");
				break;
			case "show":
				OutputCurrent();
				break;
			default:
				Output(GetHelp());
				break;
		}
	}

	private static bool TryParseFloat(string value, out float result)
	{
		return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}

	private static bool TryParseColor(List<string> args, int start, out Color color)
	{
		color = Color.white;

		// Hex form: single token like #RRGGBB or #RRGGBBAA
		if (args.Count - start == 1)
		{
			string hex = args[start];
			if (TryParseHexColor(hex, out color))
			{
				return true;
			}
			return false;
		}

		if (args.Count - start < 3)
		{
			return false;
		}

		if (!TryParseFloat(args[start], out float r) || !TryParseFloat(args[start + 1], out float g) || !TryParseFloat(args[start + 2], out float b))
		{
			return false;
		}
		float a = 1f;
		if (args.Count - start >= 4 && TryParseFloat(args[start + 3], out float parsedA))
		{
			a = parsedA;
		}

		if (r > 1f || g > 1f || b > 1f || a > 1f)
		{
			r /= 255f;
			g /= 255f;
			b /= 255f;
			a /= 255f;
		}

		color = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
		return true;
	}

	private static bool TryParseHexColor(string input, out Color color)
	{
		color = Color.white;
		if (string.IsNullOrEmpty(input))
		{
			return false;
		}

		string hex = input.Trim();
		if (hex.StartsWith("#"))
		{
			hex = hex.Substring(1);
		}

		if (hex.Length != 6 && hex.Length != 8)
		{
			return false;
		}

		try
		{
			byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			byte a = 255;
			if (hex.Length == 8)
			{
				a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}

			color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string ColorToString(Color c)
	{
		return $"{c.r:0.##},{c.g:0.##},{c.b:0.##},{c.a:0.##}";
	}

	private static void OutputCurrent()
	{
		var c = AttitudeIndicatorBehaviour.Config;
		string textState = c.ShowText ? "on" : "off";
		Output($"scale={c.Scale:0.###} movex={c.OffsetX:0.##} movey={c.OffsetY:0.##} text={textState} fontSize={c.FontSize}");
		Output($"base={ColorToString(c.BaseColor)} level={ColorToString(c.LevelColor)} font={ColorToString(c.FontColor)} bg={ColorToString(c.BgColor)}");
	}

	private static void AfterChange(string key, string value)
	{
		Output($"attui {key} -> {value}");
		AttitudeIndicatorBehaviour.NotifyConfigChanged();
	}

	private static void Output(string msg)
	{
		SdtdConsole.Instance.Output(msg);
	}
}
