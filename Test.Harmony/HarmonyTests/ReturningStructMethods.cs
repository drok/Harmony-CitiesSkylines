/*
MIT License

Copyright (c) 2017 Felix Schmidt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

 /* **************************************************************************
  * 
  * 
  * IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT
  * 
  * This file contains leftover code from the initial fork from Felix Schmidt's
  * repository https://github.com/boformer/CitiesHarmony
  * 
  * It contains known bad code, which is either not used at all in my implementation,
  * or it is in the course of being re-written. If I am rewriting it, I only included
  * it because an initial release of my project was needed urgently to address
  * a broken modding eco-system in Cities Skylines, and I considered it will do no
  * further harm over what has already been done by Felix Schmidt's code.
  * 
  * I would recommend you do not copy or rely on this code. A near future update will
  * remove this and replace it with proper code I would be proud to put my name on.
  * 
  * Until then, the copyright notice above was expressely requested by Felix Schmidt,
  * by means of a DMCA complaint at GitHub and Steam.
  * 
  * There is no code between the end of this comment and he "END-OF-Felix Schmidt-COPYRIGHT"
  * line if there is one, or the end of the file, that I, Radu Hociung, claim any copyright
  * on. The rest of the content, outside of these delimiters, is my copyright, and
  * you may copy it in accordance to the modified GPL license at the root or the repo
  * (LICENSE file)
  * 
  * FUTHER, if you base your code on a copy of the example mod from Felix Schmidt's
  * repository, which does not include his copyright notice, you will likely also
  * be a victim of DMCA complaint from him.
  */
 using HarmonyLibTests.Assets.Structs;
using System;

namespace HarmonyLibTests.Assets.Methods {
	public static class ReturningStructs_Patch {
		public static void Prefix(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
		}
	}

	public class ReturningStructs {
		public St01 IM01(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St01 { b1 = 42 };
		}

		public St02 IM02(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St02 { b1 = 42 };
		}

		public St03 IM03(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St03 { b1 = 42 };
		}

		public St04 IM04(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St04 { b1 = 42 };
		}

		public St05 IM05(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St05 { b1 = 42 };
		}

		public St06 IM06(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St06 { b1 = 42 };
		}

		public St07 IM07(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St07 { b1 = 42 };
		}

		public St08 IM08(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St08 { b1 = 42 };
		}

		public St09 IM09(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St09 { b1 = 42 };
		}

		public St10 IM10(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St10 { b1 = 42 };
		}

		public St11 IM11(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St11 { b1 = 42 };
		}

		public St12 IM12(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St12 { b1 = 42 };
		}

		public St13 IM13(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St13 { b1 = 42 };
		}

		public St14 IM14(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St14 { b1 = 42 };
		}

		public St15 IM15(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St15 { b1 = 42 };
		}

		public St16 IM16(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St16 { b1 = 42 };
		}

		public St17 IM17(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St17 { b1 = 42 };
		}

		public St18 IM18(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St18 { b1 = 42 };
		}

		public St19 IM19(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St19 { b1 = 42 };
		}

		public St20 IM20(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St20 { b1 = 42 };
		}

		//

		public static St01 SM01(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St01 { b1 = 42 };
		}

		public static St02 SM02(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St02 { b1 = 42 };
		}

		public static St03 SM03(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St03 { b1 = 42 };
		}

		public static St04 SM04(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St04 { b1 = 42 };
		}

		public static St05 SM05(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St05 { b1 = 42 };
		}

		public static St06 SM06(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St06 { b1 = 42 };
		}

		public static St07 SM07(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St07 { b1 = 42 };
		}

		public static St08 SM08(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St08 { b1 = 42 };
		}

		public static St09 SM09(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St09 { b1 = 42 };
		}

		public static St10 SM10(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St10 { b1 = 42 };
		}

		public static St11 SM11(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St11 { b1 = 42 };
		}

		public static St12 SM12(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St12 { b1 = 42 };
		}

		public static St13 SM13(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St13 { b1 = 42 };
		}

		public static St14 SM14(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St14 { b1 = 42 };
		}

		public static St15 SM15(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St15 { b1 = 42 };
		}

		public static St16 SM16(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St16 { b1 = 42 };
		}

		public static St17 SM17(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St17 { b1 = 42 };
		}

		public static St18 SM18(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St18 { b1 = 42 };
		}

		public static St19 SM19(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St19 { b1 = 42 };
		}

		public static St20 SM20(string s) {
			if (s != "test") throw new ArgumentException("First argument corrupt");
			return new St20 { b1 = 42 };
		}
	}
}