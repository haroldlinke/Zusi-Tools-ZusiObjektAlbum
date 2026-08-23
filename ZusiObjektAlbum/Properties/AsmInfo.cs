/* class AsmInfo
 *
 * Copyright (C) 2012 Holger Maaß
 *
 * This program is free software: you can redistribute it and/or modify it under 
 * the terms of the GNU General Public License as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later 
 * version. This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more 
 * details. You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using SovomaLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace ZusiObjektAlbum
{
    public static class AsmInfo
    {
        public static string Company { get; private set; }
        public static string Copyright { get; private set; }
        public static string Description { get; private set; }
        public static string Product { get; private set; }
        public static string SupportEMail { get; private set; }
        public static string Title { get; private set; }
        public static Version Version { get; private set; }

        static AsmInfo()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            Version = asm.GetName().Version;
            Company = Attribute.GetCustomAttribute(asm, typeof(AssemblyCompanyAttribute)) is AssemblyCompanyAttribute aca ? aca.Company : null;
            Description = Attribute.GetCustomAttribute(asm, typeof(AssemblyDescriptionAttribute)) is AssemblyDescriptionAttribute ada ? ada.Description : null;
            //AssemblyFileVersionAttribute vers = (AssemblyFileVersionAttribute)AssemblyVersionAttribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute));
            Copyright = Attribute.GetCustomAttribute(asm, typeof(AssemblyCopyrightAttribute)) is AssemblyCopyrightAttribute acoa ? acoa.Copyright : null;
            Product = Attribute.GetCustomAttribute(asm, typeof(AssemblyProductAttribute)) is AssemblyProductAttribute apa ? apa.Product : null;
            Title = Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute)) is AssemblyTitleAttribute ata ? ata.Title : null;
            SupportEMail = Attribute.GetCustomAttribute(asm, typeof(AssemblySupportEMail)) is AssemblySupportEMail asa ? asa.EMailAddress : null;
        }
    }
}