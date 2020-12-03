using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public sealed class MoonConsoleForm : Form
	{
		public MoonConsoleForm()
		{
			Controls.Add(new LabelEx {
				Margin = new Padding(3),
				Text = new MoonConsole().DoAThing()
			});
		}
	}
}
