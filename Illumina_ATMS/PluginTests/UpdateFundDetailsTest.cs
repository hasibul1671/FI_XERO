
using System;
using IlluminanceSolutions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PluginTests {

	[TestClass]
	public class UpdateFundDetailsTest {
		[TestMethod]
		public void TestCalcAllowance() {
			var q = new UpdateFundDetail();
			q.tracingService = new Illuminance.Commons.TraceCallingLine(null, false);

			q.CalcPendingAllowance(
				allowanceBalance: 300,
				pendingTotal: 100,
				pendingUsed: out var used,
				pendingBalance: out var balance
				);

			Assert.AreEqual(100, used);
			Assert.AreEqual(200, balance);
		}
	}
}
