namespace Terrasoft.Configuration
{

	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using Terrasoft.Common;
	using Terrasoft.Core;
	using Terrasoft.Core.Configuration;

	#region Class: VibeCodingDemoLocalizableStringsSchema

	/// <exclude/>
	public class VibeCodingDemoLocalizableStringsSchema : Terrasoft.Core.SourceCodeSchema
	{

		#region Constructors: Public

		public VibeCodingDemoLocalizableStringsSchema(SourceCodeSchemaManager sourceCodeSchemaManager)
			: base(sourceCodeSchemaManager) {
		}

		public VibeCodingDemoLocalizableStringsSchema(VibeCodingDemoLocalizableStringsSchema source)
			: base( source) {
		}

		#endregion

		#region Methods: Protected

		protected override void InitializeProperties() {
			base.InitializeProperties();
			UId = new Guid("753dcf3e-68b5-44be-9352-92d0c2fabe22");
			Name = "VibeCodingDemoLocalizableStrings";
			ParentSchemaUId = new Guid("50e3acc0-26fc-4237-a095-849a1d534bd3");
			CreatedInPackageId = new Guid("39b4c34f-ffe3-4db4-bc24-b7d451a22e73");
			ZipBody = new byte[] { 31,139,8,0,0,0,0,0,0,10,93,144,81,106,195,48,12,134,159,27,200,29,116,129,53,7,88,25,140,237,113,176,66,97,239,138,43,106,51,217,10,146,157,144,142,221,125,90,89,216,24,24,132,244,203,159,126,169,89,42,23,56,173,86,41,223,247,93,223,21,204,100,19,6,130,183,52,210,147,156,93,127,166,44,143,211,212,119,31,223,29,187,97,24,224,96,45,103,212,245,225,39,127,93,138,129,127,123,199,11,221,49,205,196,48,122,70,229,12,44,1,57,93,113,100,130,25,185,145,65,141,88,33,226,76,80,4,178,168,71,172,77,145,193,66,164,140,32,75,33,221,195,209,105,128,206,144,26,73,55,81,201,164,105,112,142,85,92,97,73,53,58,145,54,249,6,87,159,76,106,32,10,65,138,155,189,77,165,188,223,252,15,127,22,152,218,200,41,64,96,52,251,183,246,203,175,249,83,85,47,154,247,251,25,118,159,125,231,239,11,202,5,59,10,63,1,0,0 };
		}

		#endregion

		#region Methods: Public

		public override void GetParentRealUIds(Collection<Guid> realUIds) {
			base.GetParentRealUIds(realUIds);
			realUIds.Add(new Guid("753dcf3e-68b5-44be-9352-92d0c2fabe22"));
		}

		#endregion

	}

	#endregion

}
