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

	internal sealed class UserDefinedVariableData
	{
		public string Name = null;
		public bool TypeIsStr = false;
		public bool Reference = false;
		public int Dimension = 1;
		public int[] Lengths = null;
		public Int64[] DefaultInt = null;
		public string[] DefaultStr = null;
		public bool Global = false;
		public bool Save = false;
		public bool Static = true;
		public bool Private = false;
		public bool CharaData = false;
		public bool Const = false;
		
		//1822 Privateの方もDIMだけ遅延させようとしたけどちょっと課題がおおいのでやめとく
		public static UserDefinedVariableData Create(DimLineWC dimline)
		{
			return Create(dimline.WC, dimline.Dims, dimline.IsPrivate, dimline.SC);
		}

		public static UserDefinedVariableData Create(WordCollection wc, bool dims, bool isPrivate, ScriptPosition sc)
		{
			string dimtype = dims ? "#DIM" : "#DIMS";
			UserDefinedVariableData ret = new UserDefinedVariableData();
			ret.TypeIsStr = dims;

			IdentifierWord idw;
			bool staticDefined = false;
			ret.Const = false;
			string keyword = dimtype;
			//List<string> keywords;
			while (!wc.EOL && (idw = wc.Current as IdentifierWord) != null)
			{
				wc.ShiftNext();
				keyword = idw.Code;
				if (Config.ICVariable)
					keyword = keyword.ToUpper();
				//TODO ifの数があたまわるい なんとかしたい
				switch (keyword)
				{
					case "CONST":
						if (ret.CharaData)
							throw new CodeEE(keyword + "不能与CHARADATA关键字同时指定", sc);
						if (ret.Global)
							throw new CodeEE(keyword + "不能与GLOBAL关键字同时指定", sc);
						if (ret.Save)
							throw new CodeEE(keyword + "不能与SAVEDATA关键字同时指定", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "不能与REF关键字同时指定", sc);
						if (!ret.Static)
							throw new CodeEE(keyword + "不能与DYNAMIC关键字同时指定", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "关键字被重复指定", sc);
						ret.Const = true;
						break;
					case "REF":
						//throw new CodeEE("未実装の機能です", sc);
						//if (!isPrivate)
						//	throw new CodeEE("広域変数の宣言に" + keyword + "キーワードは指定できません", sc);
						if (staticDefined && ret.Static)
							throw new CodeEE(keyword + "不能与STATIC关键字同时指定", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "不能与CHARADATA关键字同时指定", sc);
						if (ret.Global)
							throw new CodeEE(keyword + "不能与GLOBAL关键字同时指定", sc);
						if (ret.Save)
							throw new CodeEE(keyword + "不能与SAVEDATA关键字同时指定", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "不能与CONST关键字同时指定", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "关键字被重复指定", sc);
						ret.Reference = true;
						ret.Static = false;
						break;
					case "DYNAMIC":
						if (!isPrivate)
							throw new CodeEE("广域变量声明不能指定" + keyword + "关键字", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "不能与CHARADATA关键字同时指定", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "不能与CONST关键字同时指定", sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE("STATIC不能与DYNAMIC关键字同时指定", sc);
							else
								throw new CodeEE(keyword + "关键字被重复指定", sc);
						staticDefined = true;
						ret.Static = false;
						break;
					case "STATIC":
						if (!isPrivate)
							throw new CodeEE("广域变量声明不能指定" + keyword + "关键字", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "不能与CHARADATA关键字同时指定", sc);
						if (staticDefined)
							if (!ret.Static)
								throw new CodeEE("STATIC不能与DYNAMIC关键字同时指定", sc);
							else
								throw new CodeEE(keyword + "关键字被重复指定", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "不能与REF关键字同时指定", sc);
						staticDefined = true;
						ret.Static = true;
						break;
					case "GLOBAL":
						if (isPrivate)
							throw new CodeEE("局部变量声明不能指定" + keyword + "关键字", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "不能与CHARADATA关键字同时指定", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "不能与REF关键字同时指定", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "不能与CONST关键字同时指定", sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE("STATIC不能与GLOBAL关键字同时指定", sc);
							else
								throw new CodeEE("DYNAMIC不能与GLOBAL关键字同时指定", sc);
						ret.Global = true;
						break;
					case "SAVEDATA":
						if (isPrivate)
							throw new CodeEE("局部变量声明不能指定" + keyword + "关键字", sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE("STATIC不能与SAVEDATA关键字同时指定", sc);
							else
								throw new CodeEE("DYNAMIC不能与SAVEDATA关键字同时指定", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "不能与REF关键字同时指定", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "不能与CONST关键字同时指定", sc);
						if (ret.Save)
							throw new CodeEE(keyword + "关键字被重复指定", sc);
						ret.Save = true;
						break;
					case "CHARADATA":
						if (isPrivate)
							throw new CodeEE("局部变量声明不能指定" + keyword + "关键字", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "不能与REF关键字同时指定", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "不能与CONST关键字同时指定", sc);
						if (staticDefined)
							if (ret.Static)
                                throw new CodeEE(keyword + "不能与STATIC关键字同时指定", sc);
							else
                                throw new CodeEE(keyword + "不能与DYNAMIC关键字同时指定", sc);
						if (ret.Global)
                            throw new CodeEE(keyword + "不能与GLOBAL关键字同时指定", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "关键字被重复指定", sc);
						ret.CharaData = true;
						break;
					default:
						ret.Name = keyword;
						goto whilebreak;
				}
			}
		whilebreak:
			if (ret.Name == null)
				throw new CodeEE(keyword + "后面没有指定有效的变量名", sc);
			string errMes = "";
			int errLevel = -1;
			if (isPrivate)
				GlobalStatic.IdentifierDictionary.CheckUserPrivateVarName(ref errMes, ref errLevel, ret.Name);
			else
				GlobalStatic.IdentifierDictionary.CheckUserVarName(ref errMes, ref errLevel, ret.Name);
			if (errLevel >= 0)
			{
				if (errLevel >= 2)
					throw new CodeEE(errMes, sc);
				ParserMediator.Warn(errMes, sc, errLevel);
			}


			List<int> sizeNum = new List<int>();
			if (wc.EOL)//サイズ省略
			{
				if (ret.Const)
					throw new CodeEE("指定了CONST关键字但未设置初始值");
				sizeNum.Add(1);
			}
			else if (wc.Current.Type == ',')//サイズ指定
			{
				while (!wc.EOL)
				{
					if (wc.Current.Type == '=')//サイズ指定解読完了＆初期値指定
						break;
					if (wc.Current.Type != ',')
						throw new CodeEE("格式错误", sc);
					wc.ShiftNext();
					if (ret.Reference)//参照型の場合は要素数不要
					{
						sizeNum.Add(0);
						if (wc.EOL)
							break;
						if (wc.Current.Type == ',')
							continue;
					}
					if (wc.EOL)
						throw new CodeEE("逗号后没有指定有效的常量表达式", sc);
					IOperandTerm arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.Comma_Assignment);
					SingleTerm sizeTerm = arg.Restructure(null) as SingleTerm;
					if ((sizeTerm == null) || (sizeTerm.GetOperandType() != typeof(Int64)))
						throw new CodeEE("逗号后没有指定有效的常量表达式", sc);
					if (ret.Reference)//参照型には要素数指定不可(0にするか書かないかどっちか
					{
						if (sizeTerm.Int != 0)
							throw new CodeEE("不能为引用型变量指定大小(请省略大小或指定为0)", sc);

						continue;
					}
					else if ((sizeTerm.Int <= 0) || (sizeTerm.Int > 1000000))
						throw new CodeEE("用户定义变量的大小必须在1到1000000之间", sc);
					sizeNum.Add((int)sizeTerm.Int);
				}
			}


			if (wc.Current.Type != '=')//初期値指定なし
			{
				if (ret.Const)
					throw new CodeEE("指定了CONST关键字但未设置初始值");
			}
			else//初期値指定あり
			{
				if (((OperatorWord)wc.Current).Code != OperatorCode.Assignment)
					throw new CodeEE("发现了预期外的运算符");
				if (ret.Reference)
					throw new CodeEE("引用型变量不能设置初始值");
				if (sizeNum.Count >= 2)
					throw new CodeEE("多维变量不能设置初始值");
				if (ret.CharaData)
					throw new CodeEE("角色型变量不能设置初始值");
				int size = 0;
				if (sizeNum.Count == 1)
					size = sizeNum[0];
				wc.ShiftNext();
				IOperandTerm[] terms = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
				if (terms.Length == 0)
					throw new CodeEE("数组的初始值不能省略");
				if (size > 0)
				{
					if (terms.Length > size)
						throw new CodeEE("初始值的数量超过了数组的大小");
					if (ret.Const && terms.Length != size)
						throw new CodeEE("常量的初始值数量与数组大小不一致");
				}
				if (dims)
					ret.DefaultStr = new string[terms.Length];
				else
					ret.DefaultInt = new Int64[terms.Length];

				for (int i = 0; i < terms.Length; i++)
				{
					if (terms[i] == null)
						throw new CodeEE("数组的初始值不能省略");
					terms[i] = terms[i].Restructure(GlobalStatic.EMediator);
					SingleTerm sTerm = terms[i] as SingleTerm;
					if (sTerm == null)
						throw new CodeEE("数组的初始值只能指定常量");
					if (dims != sTerm.IsString)
						throw new CodeEE("变量类型与初始值类型不一致");
					if (dims)
						ret.DefaultStr[i] = sTerm.Str;
					else
						ret.DefaultInt[i] = sTerm.Int;
				}
				if (sizeNum.Count == 0)
					sizeNum.Add(terms.Length);
			}
			if (!wc.EOL)
				throw new CodeEE("格式错误", sc);

			if (sizeNum.Count == 0)
				sizeNum.Add(1);

			ret.Private = isPrivate;
			ret.Dimension = sizeNum.Count;
			if (ret.Const && ret.Dimension > 1)
				throw new CodeEE("指定了CONST关键字的变量不能声明为多维数组");
			if (ret.CharaData && ret.Dimension > 2)
				throw new CodeEE("不能声明3维以上的角色型变量", sc);
			if (ret.Dimension > 3)
				throw new CodeEE("不能声明4维以上的数组变量", sc);
			ret.Lengths = new int[sizeNum.Count];
			if (ret.Reference)
				return ret;
			Int64 totalBytes = 1;
			for (int i = 0; i < sizeNum.Count; i++)
			{
				ret.Lengths[i] = sizeNum[i];
				totalBytes *= ret.Lengths[i];
			}
			if ((totalBytes <= 0) || (totalBytes > 1000000))
				throw new CodeEE("用户定义变量的大小必须在1到1000000之间", sc);
			if (!isPrivate && ret.Save && !Config.SystemSaveInBinary)
			{
				if (dims && ret.Dimension > 1)
					throw new CodeEE("字符串型多维数组变量使用SAVEDATA标记时必须开启「以二进制格式保存存档数据」选项", sc);
				else if (ret.CharaData)
					throw new CodeEE("角色型变量使用SAVEDATA标记时必须开启「以二进制格式保存存档数据」选项", sc);
			}
			return ret;
		}
	}
	internal sealed class DimLineWC
	{
		public WordCollection WC;
		public bool Dims;
		public bool IsPrivate;
		public ScriptPosition SC;
		public DimLineWC(WordCollection wc, bool isString, bool isPrivate, ScriptPosition position)
		{
			WC = wc;
			Dims = isString;
			IsPrivate = isPrivate;
			SC = position;
		}
	}

}
