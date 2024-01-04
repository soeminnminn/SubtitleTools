/* ***** BEGIN LICENSE BLOCK *****
* Version: MPL 1.1/GPL 2.0/LGPL 2.1
*
* The contents of this file are subject to the Mozilla Public License Version
* 1.1 (the "License"); you may not use this file except in compliance with
* the License. You may obtain a copy of the License at
* http://www.mozilla.org/MPL/
*
* Software distributed under the License is distributed on an "AS IS" basis,
* WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
* for the specific language governing rights and limitations under the
* License.
*
* The Original Code is mozilla.org code.
*
* The Initial Developer of the Original Code is
* Netscape Communications Corporation.
* Portions created by the Initial Developer are Copyright (C) 1998
* the Initial Developer. All Rights Reserved.
*
* Contributor(s):
*   Craig Dunn <craig dot dunn at conceptdevelopment dot net>
*
* Alternatively, the contents of this file may be used under the terms of
* either of the GNU General Public License Version 2 or later (the "GPL"),
* or the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
* in which case the provisions of the GPL or the LGPL are applicable instead
* of those above. If you wish to allow use of your version of this file only
* under the terms of either the GPL or the LGPL, and not to allow others to
* use your version of this file under the terms of the MPL, indicate your
* decision by deleting the provisions above and replace them with the notice
* and other provisions required by the GPL or the LGPL. If you do not delete
* the provisions above, a recipient may use your version of this file under
* the terms of any one of the MPL, the GPL or the LGPL.
*
* ***** END LICENSE BLOCK ***** */
/* 
 * DO NOT EDIT THIS DOCUMENT MANUALLY !!!
 * THIS FILE IS AUTOMATICALLY GENERATED BY THE TOOLS UNDER
 *    AutoDetect/tools/
 */
using System;

namespace org.mozilla.intl.chardet
{

    //import java.lang.* ;

    public class nsISO2022JPVerifier : nsVerifier
    {

        static int[] m_cclass;
        static int[] m_states;
        static int m_stFactor;
        static string m_charset;

        public override int[] cclass() { return m_cclass; }
        public override int[] states() { return m_states; }
        public override int stFactor() { return m_stFactor; }
        public override string charset() { return m_charset; }

        public nsISO2022JPVerifier()
        {

            m_cclass = new int[256 / 8];

            m_cclass[0] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (2)))))))));
            m_cclass[1] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[2] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[3] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((1) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[4] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (7))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[5] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (3)))))))));
            m_cclass[6] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[7] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[8] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (4)))) << 8) | (((int)(((0) << 4) | (6)))))))));
            m_cclass[9] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (5)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[10] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[11] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[12] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[13] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[14] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[15] = ((int)(((((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0))))))) << 16) | (((int)(((((int)(((0) << 4) | (0)))) << 8) | (((int)(((0) << 4) | (0)))))))));
            m_cclass[16] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[17] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[18] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[19] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[20] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[21] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[22] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[23] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[24] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[25] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[26] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[27] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[28] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[29] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[30] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));
            m_cclass[31] = ((int)(((((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2))))))) << 16) | (((int)(((((int)(((2) << 4) | (2)))) << 8) | (((int)(((2) << 4) | (2)))))))));



            m_states = new int[6];

            m_states[0] = ((int)(((((int)(((((int)(((eStart) << 4) | (eStart)))) << 8) | (((int)(((eStart) << 4) | (eStart))))))) << 16) | (((int)(((((int)(((eStart) << 4) | (eError)))) << 8) | (((int)(((3) << 4) | (eStart)))))))));
            m_states[1] = ((int)(((((int)(((((int)(((eError) << 4) | (eError)))) << 8) | (((int)(((eError) << 4) | (eError))))))) << 16) | (((int)(((((int)(((eError) << 4) | (eError)))) << 8) | (((int)(((eError) << 4) | (eError)))))))));
            m_states[2] = ((int)(((((int)(((((int)(((eItsMe) << 4) | (eItsMe)))) << 8) | (((int)(((eItsMe) << 4) | (eItsMe))))))) << 16) | (((int)(((((int)(((eItsMe) << 4) | (eItsMe)))) << 8) | (((int)(((eItsMe) << 4) | (eItsMe)))))))));
            m_states[3] = ((int)(((((int)(((((int)(((4) << 4) | (eError)))) << 8) | (((int)(((eError) << 4) | (eError))))))) << 16) | (((int)(((((int)(((5) << 4) | (eError)))) << 8) | (((int)(((eError) << 4) | (eError)))))))));
            m_states[4] = ((int)(((((int)(((((int)(((eError) << 4) | (eItsMe)))) << 8) | (((int)(((eError) << 4) | (eItsMe))))))) << 16) | (((int)(((((int)(((eError) << 4) | (eError)))) << 8) | (((int)(((eError) << 4) | (eError)))))))));
            m_states[5] = ((int)(((((int)(((((int)(((eError) << 4) | (eError)))) << 8) | (((int)(((eItsMe) << 4) | (eItsMe))))))) << 16) | (((int)(((((int)(((eError) << 4) | (eError)))) << 8) | (((int)(((eError) << 4) | (eError)))))))));



            m_charset = "ISO-2022-JP";
            m_stFactor = 8;

        }

        public override bool isUCS2() { return false; }


    }

} // namespace
