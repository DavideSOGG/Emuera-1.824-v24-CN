using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using System.IO;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameData.Expression;
using MinorShift._Library;

namespace MinorShift.Emuera
{
	//1756 新設。
	//また、使用されている名前を記憶し衝突を検出する。
	internal sealed class IdentifierDictionary
	{
		private enum DefinedNameType
		{
			None = 0,
			Reserved,
			SystemVariable,
			SystemMethod,
			SystemInstrument,
			//UserIdentifier,
			UserGlobalVariable,
			UserMacro,
			UserRefMethod,
			NameSpace,
		}
		readonly static char[] badSymbolAsIdentifier = new char[]
		{
			'+', '-', '*', '/', '%', '=', '!', '<', '>', '|', '&', '^', '~',
			' ', '　', '\t' ,
			'\"','(', ')', '{', '}', '[', ']', ',', '.', ':',
			'\\', '@', '$', '#', '?', ';', '\'',
			//'_'はOK
		};
		readonly static Regex regexCom = new Regex("^COM[0-9]+$");
		readonly static Regex regexComAble = new Regex("^COM_ABLE[0-9]+$");
		readonly static Regex regexAblup = new Regex("^ABLUP[0-9]+$");
		#region static
		
		public static bool IsEventLabelName(string labelName)
		{
			switch (labelName)
			{
				case "EVENTFIRST":
				case "EVENTTRAIN":
				case "EVENTSHOP":
				case "EVENTBUY":
				case "EVENTCOM":
				case "EVENTTURNEND":
				case "EVENTCOMEND":
				case "EVENTEND":
				case "EVENTLOAD":
					return true;
			}
			return false;
		}
		public static bool IsSystemLabelName(string labelName)
		{
			switch (labelName)
			{
				case "EVENTFIRST":
				case "EVENTTRAIN":
				case "EVENTSHOP":
				case "EVENTBUY":
				case "EVENTCOM":
				case "EVENTTURNEND":
				case "EVENTCOMEND":
				case "EVENTEND":
				case "SHOW_STATUS":
				case "SHOW_USERCOM":
				case "USERCOM":
				case "SOURCE_CHECK":
				case "CALLTRAINEND":
				case "SHOW_JUEL":
				case "SHOW_ABLUP_SELECT":
				case "USERABLUP":
				case "SHOW_SHOP":
				case "SAVEINFO":
				case "USERSHOP":

				case "EVENTLOAD":
				case "TITLE_LOADGAME":
				case "SYSTEM_AUTOSAVE":
				case "SYSTEM_TITLE":
				case "SYSTEM_LOADEND":
					return true;
			}

			if (labelName.StartsWith("COM"))
			{
				if (regexCom.IsMatch(labelName))
					return true;
				if (regexComAble.IsMatch(labelName))
					return true;
			}
			if (labelName.StartsWith("ABLUP"))
				if (regexAblup.IsMatch(labelName))
					return true;
			return false;
		}
		#endregion


		Dictionary<string, DefinedNameType> nameDic = new Dictionary<string, DefinedNameType>();

		List<string> privateDimList = new List<string>();
		List<string> disableList = new List<string>();
		//Dictionary<string, VariableToken> userDefinedVarDic = new Dictionary<string, VariableToken>();

		VariableData varData;
		Dictionary<string, VariableToken> varTokenDic;
		Dictionary<string, VariableLocal> localvarTokenDic;
		Dictionary<string, FunctionIdentifier> instructionDic;
		Dictionary<string, FunctionMethod> methodDic;
		Dictionary<string, UserDefinedRefMethod> refmethodDic;
		public List<UserDefinedCharaVariableToken> CharaDimList = new List<UserDefinedCharaVariableToken>();
		#region initialize
		public IdentifierDictionary(VariableData varData)
		{
			this.varData = varData;
			nameDic.Clear();
			//予約語を登録。式中に登場すると構文解析が崩壊する名前群。
			//ただしeramaker用スクリプトなら特に気にすることはない。式中に出てこない単語も同様。
			nameDic.Add("IS", DefinedNameType.Reserved);
			nameDic.Add("TO", DefinedNameType.Reserved);
			nameDic.Add("INT", DefinedNameType.Reserved);
			nameDic.Add("STR", DefinedNameType.Reserved);
			nameDic.Add("REFFUNC", DefinedNameType.Reserved);
			nameDic.Add("STATIC", DefinedNameType.Reserved);
			nameDic.Add("DYNAMIC", DefinedNameType.Reserved);
			nameDic.Add("GLOBAL", DefinedNameType.Reserved);
			nameDic.Add("PRIVATE", DefinedNameType.Reserved);
			nameDic.Add("SAVEDATA", DefinedNameType.Reserved);
			nameDic.Add("CHARADATA", DefinedNameType.Reserved);//CHARDATAから変更
			nameDic.Add("REF", DefinedNameType.Reserved);
			nameDic.Add("__DEBUG__", DefinedNameType.Reserved);
			nameDic.Add("__SKIP__", DefinedNameType.Reserved);
			nameDic.Add("_", DefinedNameType.Reserved);
			instructionDic = FunctionIdentifier.GetInstructionNameDic();

			varTokenDic = varData.GetVarTokenDicClone();
			localvarTokenDic = varData.GetLocalvarTokenDic();
			methodDic = FunctionMethodCreator.GetMethodList();
			refmethodDic = new Dictionary<string, UserDefinedRefMethod>();

			foreach(KeyValuePair<string, FunctionMethod> pair in methodDic)
			{
				nameDic.Add(pair.Key, DefinedNameType.SystemMethod);
			}

			foreach (KeyValuePair<string, VariableToken> pair in varTokenDic)
			{
				//RANDが衝突している
				//1808a3 GLOBAL、PRIVATEも
				//1808beta009 REFも
				if (!nameDic.ContainsKey(pair.Key)) 
					nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
			}

			foreach (KeyValuePair<string, VariableLocal> pair in localvarTokenDic)
			{
				nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
			}

			foreach (KeyValuePair<string, FunctionIdentifier> pair in instructionDic)
			{
				//Methodと被る
				//1808a3 SAVEDATAも
				if (!nameDic.ContainsKey(pair.Key))
					nameDic.Add(pair.Key, DefinedNameType.SystemInstrument);
			}
		}
		
		//public void SetSystemInstrumentName(List<string> names)
		//{
		//}
		
		public void CheckUserLabelName(ref string errMes, ref int warnLevel, bool isFunction, string labelName)
		{
			if (labelName.Length == 0)
			{
				errMes = "标签名不能为空";
				warnLevel = 2;
				return;
			}
			//1.721 記号をサポートしない方向に変更
			if (labelName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "标签名" + labelName + "包含\"_\"以外的符号";
				warnLevel = 1;
				return;
			}
			if (char.IsDigit(labelName[0]) && (labelName[0].ToString()).Length == LangManager.GetStrlenLang(labelName[0].ToString()))
			{
                errMes = "标签名" + labelName + "以半角数字开头";
				warnLevel = 0;
				return;
			}
			if (!isFunction || !Config.WarnFunctionOverloading)
				return;
			if (!nameDic.ContainsKey(labelName))
				return;

			if (nameDic.ContainsKey(labelName))
			{
				switch (nameDic[labelName])
				{
					case DefinedNameType.Reserved:
						if (Config.AllowFunctionOverloading)
						{
							errMes = "函数名" + labelName + "与Emuera保留字冲突，可能影响Emuera专用语法的解析";
							warnLevel = 1;
						}
						else
						{
							errMes = "函数名" + labelName + "是Emuera的保留字";
							warnLevel = 2;
						}
						break;
					case DefinedNameType.SystemMethod:
						if (Config.AllowFunctionOverloading)
						{
							errMes = "函数名" + labelName + "将覆盖Emuera的行内函数";
							warnLevel = 1;
						}
						else
						{
							errMes = "函数名" + labelName + "已用作Emuera的行内函数名";
							warnLevel = 2;
						}
						break;
					case DefinedNameType.SystemVariable:
						errMes = "函数名" + labelName + "已用作Emuera的变量名";
						warnLevel = 1;
						break;
					case DefinedNameType.SystemInstrument:
						errMes = "函数名" + labelName + "已用作Emuera的变量或指令名";
						warnLevel = 1;
						break;
					case DefinedNameType.UserMacro:
						//字句解析がうまくいっていれば本来あり得ないはず
						errMes = "函数名" + labelName + "已用作宏名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "函数名" + labelName + "已用作引用型函数名称";
						warnLevel = 2;
						break;
				}
			}
		}
		
		public void CheckUserVarName(ref string errMes, ref int warnLevel, string varName)
		{
			//if (varName.Length == 0)
			//{
			//    errMes = "变量名不能为空";
			//    warnLevel = 2;
			//    return;
			//}
			//1.721 記号をサポートしない方向に変更
			if (varName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "变量名" + varName + "包含\"_\"以外的符号";
				warnLevel = 2;
				return;
			}
			//if (char.IsDigit(varName[0]))
			//{
			//    errMes = "变量名" + varName + "以半角数字开头";
			//    warnLevel = 2;
			//    return;
			//}

			if (nameDic.ContainsKey(varName))
			{
				switch (nameDic[varName])
				{
					case DefinedNameType.Reserved:
						errMes = "变量名" + varName + "是Emuera的保留字";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//代入文が使えなくなるために指令名との衝突は致命的。
						errMes = "变量名" + varName + "已用作Emuera的指令名";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemVariable:
						errMes = "变量名" + varName + "已用作Emuera的变量名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						errMes = "变量名" + varName + "已用作宏名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						errMes = "变量名" + varName + "已用作用户定义的全局变量名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "变量名" + varName + "已用作引用型函数名称";
						warnLevel = 2;
						break;
				}
			}
		}

		public void CheckUserMacroName(ref string errMes, ref int warnLevel, string macroName)
		{
			if (macroName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "宏名" + macroName + "包含\"_\"以外的符号";
				warnLevel = 2;
				return;
			}
			if (nameDic.ContainsKey(macroName))
			{
				switch (nameDic[macroName])
				{
					case DefinedNameType.Reserved:
						errMes = "宏名" + macroName + "是Emuera的保留字";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//指令名を上書きした時が面倒なのでとりあえず許可しない
						errMes = "宏名" + macroName + "已用作Emuera的指令名";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemVariable:
						//別に上書きしてもいいがとりあえず許可しないでおく。いずれ解放するかもしれない
						errMes = "宏名" + macroName + "已用作Emuera的变量名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						errMes = "宏名" + macroName + "已用作宏名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						errMes = "宏名" + macroName + "已用作用户定义的全局变量名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "宏名" + macroName + "已用作引用型函数名称";
						warnLevel = 2;
						break;
				}
			}
		}

		public void CheckUserPrivateVarName(ref string errMes, ref int warnLevel, string varName)
		{
			if (varName.Length == 0)
			{
				errMes = "变量名不能为空";
				warnLevel = 2;
				return;
			}
			//1.721 記号をサポートしない方向に変更
			if (varName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "变量名" + varName + "包含\"_\"以外的符号";
				warnLevel = 2;
				return;
			}
			if (char.IsDigit(varName[0]))
			{
				errMes = "变量名" + varName + "以半角数字开头";
				warnLevel = 2;
				return;
			}
			if(nameDic.ContainsKey(varName))
			{
				switch(nameDic[varName])
				{
					case DefinedNameType.Reserved:
						errMes = "变量名" + varName + "是Emuera的保留字";
						warnLevel = 2;
						return;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//代入文が使えなくなるために指令名との衝突は致命的。
						errMes = "变量名" + varName + "已用作Emuera的指令名";
						warnLevel = 2;
						return;
					case DefinedNameType.SystemVariable:
						//システム变量の上書きは不可
                        errMes = "变量名" + varName + "已用作Emuera的变量名";
                        warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						//字句解析がうまくいっていれば本来あり得ないはず
						errMes = "变量名" + varName + "已用作宏名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						//広域变量の上書きは禁止しておく
						errMes = "变量名" + varName + "已用作用户定义的全局变量名";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "变量名" + varName + "已用作引用型函数名称";
						warnLevel = 2;
						break;
                }
			}
			privateDimList.Add(varName);
		}
		#endregion

		#region header.erb
		//1807 ErbLoaderに移動
		Dictionary<string, DefineMacro> macroDic = new Dictionary<string, DefineMacro>();

		internal void AddUseDefinedVariable(VariableToken var)
		{
			varTokenDic.Add(var.Name, var);
			if (var.IsCharacterData)
			{

			}
			nameDic.Add(var.Name, DefinedNameType.UserGlobalVariable);
		}
		internal void AddMacro(DefineMacro mac)
		{
			nameDic.Add(mac.Keyword, DefinedNameType.UserMacro);
			macroDic.Add(mac.Keyword, mac);
		}
		internal void AddRefMethod(UserDefinedRefMethod refm)
		{
			refmethodDic.Add(refm.Name, refm);
			nameDic.Add(refm.Name, DefinedNameType.UserRefMethod);
		}
		#endregion

		#region get

		public bool UseMacro()
		{
			return macroDic.Count > 0;
		}

		public DefineMacro GetMacro(string key)
		{
			if (Config.ICVariable)
				key = key.ToUpper();
			if (macroDic.ContainsKey(key))
				return macroDic[key];
			return null;
		}

		public VariableToken GetVariableToken(string key, string subKey, bool allowPrivate)
		{
			VariableToken ret;
            if (Config.ICVariable)
                key = key.ToUpper();
            if (allowPrivate)
			{
				LogicalLine line = GlobalStatic.Process.GetScaningLine();
				if ((line != null) && (line.ParentLabelLine != null))
				{
					ret = line.ParentLabelLine.GetPrivateVariable(key);
					if(ret != null)
					{
						if (subKey != null)
							throw new CodeEE("私有变量" + key + "使用了@");
						return ret;
					}
				}
			}
			if (localvarTokenDic.ContainsKey(key))
			{
				if (localvarTokenDic[key].IsForbid)
                {
					throw new CodeEE("调用的变量\"" + key + "\"已被设置禁止使用");
                }
				LogicalLine line = GlobalStatic.Process.GetScaningLine();
				if (string.IsNullOrEmpty(subKey))
				{
					//システムの入力待ち中にデバッグコマンドからLOCALを呼んだとき。
					if ((line == null) || (line.ParentLabelLine == null))
						throw new CodeEE("没有正在执行的函数，因此无法获取或修改" + key);
					subKey = line.ParentLabelLine.LabelName;
				}
				else
				{
					ParserMediator.Warn("不建议在代码中使用@调用局部变量(请考虑改用*.ERH文件)", line, 1, false, false);
					if (Config.ICFunction)
						subKey = subKey.ToUpper();
				}
                LocalVariableToken retLocal = localvarTokenDic[key].GetExistLocalVariableToken(subKey);
                if (retLocal == null)
                    retLocal = localvarTokenDic[key].GetNewLocalVariableToken(subKey, line.ParentLabelLine);
                return retLocal;
			}
			if (varTokenDic.TryGetValue(key, out ret))
			{
                //一文字变量の禁止オプションを考えた名残
                //if (Config.ForbidOneCodeVariable && ret.CanForbid)
                //    throw new CodeEE("設定によりシステム一文字数値变量の使用が禁止されています(调用的变量：" + ret.Name +")");
                if (ret.IsForbid)
                {
					if(!ret.CanForbid)
						throw new ExeEE("不支持CanForbid的变量\"" + ret.Name +"\"被设置了IsForbid");
                    throw new CodeEE("调用的变量\"" + ret.Name +"\"已被设置禁止使用");
                }
				if (subKey != null)
					throw new CodeEE("非局部变量" + key + "使用了@");
                return ret;
            }
			if (subKey != null)
				throw new CodeEE("@的使用方式不正确");
			return null;
		}

		public FunctionIdentifier GetFunctionIdentifier(string str)
		{
			string key = str;
            if (string.IsNullOrEmpty(key))
                return null;
            if (Config.ICFunction)
				key = key.ToUpper();
			if (instructionDic.TryGetValue(key, out FunctionIdentifier ret))
				return ret;
			else
				return null;
		}

		public List<string> GetOverloadedList(LabelDictionary labelDic)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, FunctionMethod> pair in methodDic)
			{
				FunctionLabelLine func = labelDic.GetNonEventLabel(pair.Key);
				if (func == null)
					continue;
				if (!func.IsMethod)
					continue;
				list.Add(pair.Key);
			}
			return list;
		}

		public UserDefinedRefMethod GetRefMethod(string codeStr)
		{
			if (Config.ICFunction)
				codeStr = codeStr.ToUpper();
			if (refmethodDic.ContainsKey(codeStr))
				return refmethodDic[codeStr];
			return null;
		}

		public IOperandTerm GetFunctionMethod(LabelDictionary labelDic, string codeStr, IOperandTerm[] arguments, bool userDefinedOnly)
		{
			if (Config.ICFunction)
				codeStr = codeStr.ToUpper();
			if (arguments == null)//引数なし、名前のみの探索
			{
				if (refmethodDic.ContainsKey(codeStr))
					return new UserDefinedRefMethodNoArgTerm(refmethodDic[codeStr]);
				return null;
			}
			if ((labelDic != null) && (labelDic.Initialized))
			{
				if (refmethodDic.ContainsKey(codeStr))
					return new UserDefinedRefMethodTerm(refmethodDic[codeStr], arguments);
				FunctionLabelLine func = labelDic.GetNonEventLabel(codeStr);
				if (func != null)
				{
					if (userDefinedOnly && !func.IsMethod)
					{
						throw new CodeEE("试图通过CALLF系指令调用未指定#FUNCTION的函数\"@" + func.LabelName + "\"");
					}
					if (func.IsMethod)
					{
						string errMes;
						IOperandTerm ret = UserDefinedMethodTerm.Create(func, arguments, out errMes);
						if(ret == null)
							throw new CodeEE(errMes);
						return ret;
					}
					//1.721 #FUNCTIONが定義されていない関数は組み込み関数を上書きしない方向に。 PANCTION.ERBのRANDとか。
					if (!methodDic.ContainsKey(codeStr))
						throw new CodeEE("试图在表达式中调用未定义#FUNCTION的函数(" + func.Position.Filename + ":" + func.Position.LineNo + "行)");
				}
			}
			if (userDefinedOnly)
				return null;
            if (!methodDic.TryGetValue(codeStr, out FunctionMethod method))
                return null;
            string errmes = method.CheckArgumentType(codeStr, arguments);
			if (errmes != null)
				throw new CodeEE(errmes);
			return new FunctionMethodTerm(method, arguments);
		}

		//1756 作成中途
		//名前リストを元に何がやりたかったのかを推定してCodeEEを投げる
		//1822 DIMリストの解決中にIdentifierNotFoundCodeEEが飛んだ場合にはやり直しの可能性がある
		public void ThrowException(string str, bool isFunc)
		{
			string idStr = str;
			if(Config.ICFunction || Config.ICVariable) //片方だけなのは互換性用オプションなのでレアケースのはず。対応しない。
				idStr = idStr.ToUpper();
			if (disableList.Contains(idStr))
				throw new CodeEE("\"" + str + "\"已声明#DISABLE");
			if (!isFunc && privateDimList.Contains(idStr))
				throw new IdentifierNotFoundCodeEE("变量\"" + str + "\"未在此函数中定义");
			if (nameDic.ContainsKey(idStr))
			{
				DefinedNameType type = nameDic[idStr];
				switch (type)
				{
					case DefinedNameType.Reserved:
						throw new CodeEE("Emuera保留字\"" + str + "\"的使用方式不正确");
					case DefinedNameType.SystemVariable:
					case DefinedNameType.UserGlobalVariable:
						if (isFunc)
							throw new CodeEE("变量名\"" + str + "\"被当作函数使用");
						break;
					case DefinedNameType.SystemMethod:
					case DefinedNameType.UserRefMethod:
						if (!isFunc)
							throw new CodeEE("函数名\"" + str + "\"被当作变量使用");
						break;
					case DefinedNameType.UserMacro:
						throw new CodeEE("意外的宏名\"" + str + "\"");
					case DefinedNameType.SystemInstrument:
						if (isFunc)
							throw new CodeEE("指令名\"" + str + "\"被当作函数使用");
						else
							throw new CodeEE("指令名\"" + str + "\"被当作变量使用");
			
				}
			}
			throw new IdentifierNotFoundCodeEE("\"" + idStr + "\"是无法识别的标识符");
		}
		#endregion

        #region util
        public void resizeLocalVars(string key, string subKey, int newSize)
        {
            localvarTokenDic[key].ResizeLocalVariableToken(subKey, newSize);
        }

        public int getLocalDefaultSize(string key)
        {
            return localvarTokenDic[key].GetDefaultSize();
        }

		public bool getLocalIsForbid(string key)
		{
			return localvarTokenDic[key].IsForbid;
		}
        public bool getVarTokenIsForbid(string key)
        {
            if (localvarTokenDic.ContainsKey(key))
                return localvarTokenDic[key].IsForbid;
			varTokenDic.TryGetValue(key, out VariableToken var);
            if (var != null)
                    return var.IsForbid;
            return true;
        }
        #endregion


	}
}
