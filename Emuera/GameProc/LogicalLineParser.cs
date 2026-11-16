using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc.Function;

namespace MinorShift.Emuera.GameProc
{
	internal static class LogicalLineParser
	{
		public static bool ParseSharpLine(FunctionLabelLine label, StringStream st, ScriptPosition position, List<string> OnlyLabel)
		{
			st.ShiftNext();//'#'を飛ばす
			string token = LexicalAnalyzer.ReadSingleIdentifier(st);//#～自体にはマクロ非適用
			if (Config.ICFunction)
				token = token.ToUpper();
            //#行として不正な行でもAnalyzeに行って引っかかることがあるので、先に存在しない#～は弾いてしまう
            if (token == null || (token != "SINGLE" && token != "LATER" && token != "PRI" && token != "ONLY" && token != "FUNCTION" && token != "FUNCTIONS" 
                && token != "LOCALSIZE" && token != "LOCALSSIZE" && token != "DIM" && token != "DIMS"))
            {
                ParserMediator.Warn("无法解析的#行", position, 1);
                return false;
            }
			try
			{
				WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
				switch (token)
				{
					case "SINGLE":
						if (label.IsMethod)
						{
							ParserMediator.Warn("行内函数中#SINGLE不会生效", position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn("非事件函数中#SINGLE不会生效", position, 1);
							break;
						}
						else if (label.IsSingle)
						{
							ParserMediator.Warn("#SINGLE被重复使用", position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn("指定了#ONLY的事件函数中#SINGLE不会生效", position, 1);
							break;
						}
						label.IsSingle = true;
						break;
					case "LATER":
						if (label.IsMethod)
						{
							ParserMediator.Warn("行内函数中#LATER不会生效", position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn("非事件函数中#LATER不会生效", position, 1);
							break;
						}
						else if (label.IsLater)
						{
							ParserMediator.Warn("#LATER被重复使用", position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn("指定了#ONLY的事件函数中#LATER不会生效", position, 1);
							break;
						}
						else if (label.IsPri)
							ParserMediator.Warn("#PRI与#LATER被同时使用(此函数会被调用2次)", position, 1);
						label.IsLater = true;
						break;
					case "PRI":
						if (label.IsMethod)
						{
							ParserMediator.Warn("行内函数中#PRI不会生效", position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn("非事件函数中#PRI不会生效", position, 1);
							break;
						}
						else if (label.IsPri)
						{
							ParserMediator.Warn("#PRI被重复使用", position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn("指定了#ONLY的事件函数中#PRI不会生效", position, 1);
							break;
						}
						else if (label.IsLater)
							ParserMediator.Warn("#PRI与#LATER被同时使用(此函数会被调用2次)", position, 1);
						label.IsPri = true;
						break;
					case "ONLY":
						if (label.IsMethod)
						{
							ParserMediator.Warn("行内函数中#ONLY不会生效", position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn("非事件函数中#ONLY不会生效", position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn("#ONLY被重复使用", position, 1);
							break;
						}
						else if (OnlyLabel.Contains(label.LabelName))
							ParserMediator.Warn("事件函数\"@" + label.LabelName + "\"已经声明过#ONLY（此函数不会被执行）", position, 1);
						OnlyLabel.Add(label.LabelName);
						label.IsOnly = true;
						if (label.IsPri)
						{
							ParserMediator.Warn("此事件函数中声明了#PRI，但将被忽略", position, 1);
							label.IsPri = false;
						}
						if (label.IsLater)
						{
							ParserMediator.Warn("此事件函数中声明了#LATER，但将被忽略", position, 1);
							label.IsLater = false;
						}
						if (label.IsSingle)
						{
							ParserMediator.Warn("此事件函数中声明了#SINGLE，但将被忽略", position, 1);
							label.IsSingle = false;
						}
						break;
					case "FUNCTION":
					case "FUNCTIONS":
						if (!string.IsNullOrEmpty(label.LabelName) && char.IsDigit(label.LabelName[0]))
						{
							ParserMediator.Warn("#" + token + "属性不能指定给以数字开头的函数名", position, 1);
							label.IsError = true;
							label.ErrMes = "函数名以数字开头";
							break;
						}
						if (label.IsMethod)
						{
							if ((label.MethodType == typeof(Int64) && token == "FUNCTION") || (label.MethodType == typeof(string) && token == "FUNCTIONS"))
							{
								ParserMediator.Warn("函数" + label.LabelName + "已经声明过#" + token + "(此行将被忽略)", position, 1);
								return false;
							}
							if (label.MethodType == typeof(Int64) && token == "FUNCTIONS")
								ParserMediator.Warn("函数" + label.LabelName + "已经声明过#FUNCTION", position, 2);
							else if (label.MethodType == typeof(string) && token == "FUNCTION")
								ParserMediator.Warn("函数" + label.LabelName + "已经声明过#FUNCTIONS", position, 2);
							return false;
						}
						if (label.Depth == 0)
						{
							ParserMediator.Warn("对系统函数指定了#" + token, position, 2);
							return false;
						}
						label.IsMethod = true;
						label.Depth = 0;
						if (token == "FUNCTIONS")
							label.MethodType = typeof(string);
						else
							label.MethodType = typeof(Int64);
						if (label.IsPri)
						{
							ParserMediator.Warn("行内函数中#PRI不会生效", position, 1);
							label.IsPri = false;
						}
						if (label.IsLater)
						{
							ParserMediator.Warn("行内函数中#LATER不会生效", position, 1);
							label.IsLater = false;
						}
						if (label.IsSingle)
						{
							ParserMediator.Warn("行内函数中#SINGLE不会生效", position, 1);
							label.IsSingle = false;
						}
						if (label.IsOnly)
						{
							ParserMediator.Warn("行内函数中#ONLY不会生效", position, 1);
							label.IsOnly = false;
						}
						break;
					case "LOCALSIZE":
					case "LOCALSSIZE":
						{
							if (wc.EOL)
							{
								ParserMediator.Warn("#" + token + "后面没有指定有效的数值", position, 2);
								break;
							}
                            //イベント関数では指定しても無視される
                            if (label.IsEvent)
                            {
                                ParserMediator.Warn("事件函数中通过#" + token + "指定的" + token.Substring(0, token.Length - 4) + "大小将被忽略", position, 1);
                                break;
                            }
							IOperandTerm arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL);
                            if ((!(arg.Restructure(null) is SingleTerm sizeTerm)) || (sizeTerm.GetOperandType() != typeof(Int64)))
                            {
                                ParserMediator.Warn("#" + token + "后面没有指定有效的常量表达式", position, 2);
                                break;
                            }
                            if (sizeTerm.Int <= 0)
							{
								ParserMediator.Warn("向#" + token + "传递了不大于0的值(" + sizeTerm.Int.ToString() + ")。设置将被忽略", position, 1);
								break;
							}
							if (sizeTerm.Int >= Int32.MaxValue)
							{
								ParserMediator.Warn("向#" + token + "传递了过大的值(" + sizeTerm.Int.ToString() + ")。设置将被忽略", position, 1);
								break;
							}
							int size = (int)sizeTerm.Int;
							if (token == "LOCALSIZE")
							{
								if (GlobalStatic.IdentifierDictionary.getLocalIsForbid("LOCAL"))
								{
									ParserMediator.Warn("指定了#" + token + "，但变量LOCAL已被禁止使用", position, 2);
									break;
								}
								if (label.LocalLength > 0)
									ParserMediator.Warn("此函数已经定义过#LOCALSIZE。（之前的定义将被忽略）", position, 1);
								label.LocalLength = size;
							}
							else
							{
								if (GlobalStatic.IdentifierDictionary.getLocalIsForbid("LOCALS"))
								{
									ParserMediator.Warn("指定了#" + token + "，但变量LOCALS已被禁止使用", position, 2);
									break;
								}
								if (label.LocalsLength > 0)
									ParserMediator.Warn("此函数已经定义过#LOCALSSIZE。（之前的定义将被忽略）", position, 1);
								label.LocalsLength = size;
							}
						}
						break;
					case "DIM":
					case "DIMS":
						{
							UserDefinedVariableData data = UserDefinedVariableData.Create(wc, token == "DIMS", true, position);
							if (!label.AddPrivateVariable(data))
							{
								ParserMediator.Warn("变量名" + data.Name + "已被使用", position, 2);
								return false;
							}
							break;
						}
					default:
						ParserMediator.Warn("无法解析的#行", position, 1);
						break;
				}
				if (!wc.EOL)
					ParserMediator.Warn("#的标识符后存在多余的字符", position, 1);
			}
			catch (Exception e)
			{
				ParserMediator.Warn(e.Message, position, 2);
				goto err;
			}
			return true;
		err:
			return false;
		}
		
		public static LogicalLine ParseLine(string str, EmueraConsole console)
		{
			ScriptPosition position = new ScriptPosition();
			StringStream stream = new StringStream(str);
			return ParseLine(stream, position, console);
		}

		public static LogicalLine ParseLabelLine(StringStream stream, ScriptPosition position, EmueraConsole console)
		{
			bool isFunction = (stream.Current == '@');
			//int lineNo = position.LineNo;
			string labelName = "";
			string errMes = "";
			try
			{
				int warnLevel = -1;
                stream.ShiftNext();//@か$を除去
				WordCollection wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
				if (wc.EOL || !(wc.Current is IdentifierWord))
				{
					errMes = "函数名不正确或不存在";
					goto err;
				}
				labelName = ((IdentifierWord)wc.Current).Code;
				wc.ShiftNext();
				if (Config.ICVariable)
					labelName = labelName.ToUpper();
				GlobalStatic.IdentifierDictionary.CheckUserLabelName(ref errMes, ref warnLevel, isFunction, labelName);
				if (warnLevel >= 0)
				{
					if (warnLevel >= 2)
						goto err;
					ParserMediator.Warn(errMes, position, warnLevel);
				}
				if (!isFunction)//$ならこの時点で終了
				{
					if (!wc.EOL)
						ParserMediator.Warn("以$开头的标签设置了参数", position, 1);
					return new GotoLabelLine(position, labelName);
				}



				//labelName = LexicalAnalyzer.ReadString(stream, StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon);
				//labelName = labelName.Trim();
				//if (Config.ICVariable)
				//    labelName = labelName.ToUpper();
				//GlobalStatic.IdentifierDictionary.CheckUserLabelName(ref errMes, ref warnLevel, isFunction, labelName);
				//if(warnLevel >= 0)
				//{
				//    if (warnLevel >= 2)
				//        goto err;
				//    ParserMediator.Warn(errMes, position, warnLevel);
				//}
				//if (!isFunction)//$ならこの時点で終了
				//{
				//    LexicalAnalyzer.SkipWhiteSpace(stream);
				//    if (!stream.EOS)
				//        ParserMediator.Warn("$で始まるラベルに引数が設定されています", position, 1);
				//    return new GotoLabelLine(position, labelName);
				//}

				////関数名部分に_renameを使えないように変更
				//if (ParserMediator.RenameDic != null && ((stream.ToString().IndexOf("[[") >= 0) && (stream.ToString().IndexOf("]]") >= 0)))
				//{
				//    string line = stream.ToString();
				//    foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
				//        line = line.Replace(pair.Key, pair.Value);
				//    stream = new StringStream(line);
				//}
				//WordCollection wc = null;
				//wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
				if (Program.AnalysisMode)
					console.PrintC("@" + labelName, false);
				FunctionLabelLine funclabelLine = new FunctionLabelLine(position, labelName, wc);
				if (IdentifierDictionary.IsEventLabelName(labelName))
				{
					funclabelLine.IsEvent = true;
					funclabelLine.IsSystem = true;
					funclabelLine.Depth = 0;
				}
				else if (IdentifierDictionary.IsSystemLabelName(labelName))
				{
					funclabelLine.IsSystem = true;
					funclabelLine.Depth = 0;
				}
				return funclabelLine;
			}
			catch (CodeEE e)
			{
				errMes = e.Message;
			}
		err:
			System.Media.SystemSounds.Hand.Play();
			if (isFunction)
			{
				if(labelName.Length == 0)
					labelName = "<Error>";
				return new InvalidLabelLine(position, labelName, errMes);
			}
			return new InvalidLine(position, errMes);
		}
		
		
		public static LogicalLine ParseLine(StringStream stream, ScriptPosition position, EmueraConsole console)
		{
			//int lineNo = position.LineNo;
			string errMes;
			LexicalAnalyzer.SkipWhiteSpace(stream);//先頭のホワイトスペースを読み飛ばす
			if (stream.EOS)
				return null;
			//コメント行かどうかはここに来る前に判定しておく
			try
			{
				#region 前置インクリメント、デクリメント行
				if (stream.Current == '+' || stream.Current == '-')
				{
					char op = stream.Current;
					WordCollection wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.None);
                    if ((!(wc.Current is OperatorWord opWT)) || ((opWT.Code != OperatorCode.Increment) && (opWT.Code != OperatorCode.Decrement)))
                    {
                        if (op == '+')
                            errMes = "行以\'+\'开头，但不是有效的自增运算";
                        else
                            errMes = "行以\'-\'开头，但不是有效的自减运算";
                        goto err;
                    }
                    wc.ShiftNext();
					//token = EpressionParser.単語一個分取得(wc)
					//token非変数
					//token文字列形
					//token変更不可能
					//if (wc != EOS)
					//
					return new InstructionLine(position, FunctionIdentifier.SETFunction, opWT.Code, wc, null);
				}
				#endregion
				IdentifierWord idWT = LexicalAnalyzer.ReadFirstIdentifierWord(stream);
				if (idWT != null)
				{
					FunctionIdentifier func = GlobalStatic.IdentifierDictionary.GetFunctionIdentifier(idWT.Code);
					//命令文
					if (func != null)//関数文
					{
						if (stream.EOS) //引数の無い関数
							return new InstructionLine(position, func, stream);
						if ((stream.Current != ';') && (stream.Current != ' ') && (stream.Current != '\t') && (!Config.SystemAllowFullSpace || (stream.Current != '　')))
						{
							if (stream.Current == '　')
								errMes = "行以命令开头，但在命令后紧跟了半角空格、制表符以外的字符(该警告可通过系统选项「" + Config.GetConfigName(ConfigCode.SystemAllowFullSpace) + "」忽略)";
							else
								errMes = "行以命令开头，但在命令后紧跟了半角空格、制表符以外的字符";
							goto err;
						}
						stream.ShiftNext();
						return new InstructionLine(position, func, stream);
					}
				}
				LexicalAnalyzer.SkipWhiteSpace(stream);
				if (stream.EOS)
				{
					errMes = "无法解析的行";
					goto err;
				}
				//命令行ではない→代入行のはず
				stream.Seek(0, System.IO.SeekOrigin.Begin);
				OperatorCode assignOP = OperatorCode.NULL;
				WordCollection wc1 = LexicalAnalyzer.Analyse(stream, LexEndWith.Operator, LexAnalyzeFlag.None);
				//if (idWT != null)
				//	wc1.Collection.Insert(0, idWT);
				try
				{
					assignOP = LexicalAnalyzer.ReadAssignmentOperator(stream);
				}
				catch(CodeEE)
				{
					errMes = "无法解析的行";
					goto err;
				}
				//eramaker互換警告
				//stream.Jump(-1);
				//if ((stream.Current != ' ') && (stream.Current != '\t'))
				//{
				//	errMes = "変数で行が始まっていますが、演算子の直前に半角スペースまたはタブがありません";
				//	goto err;
				//}
				//stream.ShiftNext();


				if (assignOP == OperatorCode.Equal)
				{
					if (console != null)
						ParserMediator.Warn("将\"==\"用作了赋值运算符", position, 0);
					//"=="を代入文に使うのは本当はおかしいが結構使われているので仕様にする
					assignOP = OperatorCode.Assignment;
				}
				return new InstructionLine(position, FunctionIdentifier.SETFunction, assignOP, wc1, stream);
			err:
				return new InvalidLine(position, errMes);
			}
			catch (CodeEE e)
			{
				System.Media.SystemSounds.Hand.Play();
				return new InvalidLine(position, e.Message);
			}
		}
		
	}
}
