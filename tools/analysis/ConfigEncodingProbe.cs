using System;
using System.IO;
using System.Reflection;
using System.Text;

internal static class ConfigEncodingProbe
{
	private const BindingFlags AllFlags =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

	public static int Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		if (args.Length != 1)
			throw new ArgumentException("Expected the game directory path.");

		string gameDir = Path.GetFullPath(args[0]);
		string exePath = Path.Combine(gameDir, "Emuera.exe");
		Assembly assembly = Assembly.LoadFrom(exePath);

		Type programType = assembly.GetType("MinorShift.Emuera.Program", true);
		SetStaticProperty(programType, "ExeDir", gameDir + "\\");
		SetStaticProperty(programType, "CsvDir", Path.Combine(gameDir, "CSV") + "\\");
		SetStaticProperty(programType, "ErbDir", Path.Combine(gameDir, "ERB") + "\\");
		SetStaticProperty(programType, "DebugDir", Path.Combine(gameDir, "debug") + "\\");
		SetStaticProperty(programType, "DatDir", Path.Combine(gameDir, "dat") + "\\");
		SetStaticProperty(programType, "ContentDir", Path.Combine(gameDir, "resources") + "\\");

		Type configDataType = assembly.GetType("MinorShift.Emuera.ConfigData", true);
		object configData = configDataType.GetProperty("Instance", AllFlags).GetValue(null, null);
		MethodInfo loadConfig = configDataType.GetMethod("LoadConfig", AllFlags);
		MethodInfo saveConfig = configDataType.GetMethod("SaveConfig", AllFlags);
		loadConfig.Invoke(configData, null);

		Type configCodeType = assembly.GetType("MinorShift.Emuera.ConfigCode", true);
		MethodInfo getConfigItem = configDataType.GetMethod(
			"GetConfigItem",
			AllFlags,
			null,
			new[] { configCodeType },
			null);
		SetBooleanConfig(getConfigItem, configData, configCodeType, "SystemReadInUTF8");
		SetBooleanConfig(getConfigItem, configData, configCodeType, "SystemWriteInUTF8");

		if (!(bool)saveConfig.Invoke(configData, null))
			throw new InvalidOperationException("SaveConfig returned false.");
		loadConfig.Invoke(configData, null);

		Type configType = assembly.GetType("MinorShift.Emuera.Config", true);
		Encoding gameEncoding = (Encoding)GetStaticField(configType, "Encode");
		Encoding configEncoding = (Encoding)GetStaticField(configType, "ReadEncode");
		Encoding writeEncoding = (Encoding)GetStaticField(configType, "WriteEncode");

		Type parserType = assembly.GetType("MinorShift.Emuera.ParserMediator", true);
		bool hasWarning = (bool)parserType.GetProperty("HasWarning", AllFlags).GetValue(null, null);

		const string probeText = "日本語テスト";
		string probePath = Path.Combine(Path.GetTempPath(), "emuera-shiftjis-" + Guid.NewGuid().ToString("N") + ".txt");
		File.WriteAllText(probePath, probeText, Encoding.GetEncoding(932));
		string actualText;
		try
		{
			Type readerType = assembly.GetType("MinorShift.Emuera.Sub.EraStreamReader", true);
			object reader = Activator.CreateInstance(
				readerType,
				AllFlags,
				null,
				new object[] { false },
				null);
			try
			{
				MethodInfo open = readerType.GetMethod("Open", AllFlags, null, new[] { typeof(string) }, null);
				if (!(bool)open.Invoke(reader, new object[] { probePath }))
					throw new InvalidOperationException("EraStreamReader failed to open the Shift-JIS probe.");
				actualText = (string)readerType.GetMethod("ReadLine", AllFlags).Invoke(reader, null);
			}
			finally
			{
				readerType.GetMethod("Dispose", AllFlags).Invoke(reader, null);
			}
		}
		finally
		{
			File.Delete(probePath);
		}

		string configPath = Path.Combine(gameDir, "emuera.config");
		byte[] configBytes = File.ReadAllBytes(configPath);
		string configText = Encoding.UTF8.GetString(configBytes);
		string bom = string.Format("{0:X2} {1:X2} {2:X2}", configBytes[0], configBytes[1], configBytes[2]);

		Console.WriteLine("GameEncoding={0}", gameEncoding.WebName);
		Console.WriteLine("ConfigReadEncoding={0}", configEncoding.WebName);
		Console.WriteLine("ConfigWriteEncoding={0}", writeEncoding.WebName);
		Console.WriteLine("ConfigHasWarning={0}", hasWarning);
		Console.WriteLine("ConfigBom={0}", bom);
		Console.WriteLine("ConfigHasChineseKeys={0}",
			configText.Contains("忽略大小写") &&
			configText.Contains("配置、宏和日志默认以UTF-8编码读入"));
		Console.WriteLine("ShiftJisProbeExpected={0}", probeText);
		Console.WriteLine("ShiftJisProbeActual={0}", actualText);
		Console.WriteLine("ShiftJisProbePassed={0}", actualText == probeText);

		return gameEncoding.WebName == "shift_jis" &&
			configEncoding.WebName == "utf-8" &&
			writeEncoding.WebName == "utf-8" &&
			!hasWarning &&
			bom == "EF BB BF" &&
			actualText == probeText
			? 0
			: 1;
	}

	private static void SetBooleanConfig(
		MethodInfo getConfigItem,
		object configData,
		Type configCodeType,
		string codeName)
	{
		object code = Enum.Parse(configCodeType, codeName);
		object item = getConfigItem.Invoke(configData, new[] { code });
		item.GetType().GetProperty("Value", AllFlags).SetValue(item, true, null);
	}

	private static void SetStaticProperty(Type type, string name, string value)
	{
		type.GetProperty(name, AllFlags).SetValue(null, value, null);
	}

	private static object GetStaticField(Type type, string name)
	{
		return type.GetField(name, AllFlags).GetValue(null);
	}
}
