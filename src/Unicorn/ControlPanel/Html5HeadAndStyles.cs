using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class Html5HeadAndStyles : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write(@"<!DOCTYPE html>

			<html>
			<head>
				<title>Unicorn Control Panel</title>
				<style>
					* { font-family: sans-serif; }
					body { max-width: 960px; margin: 0 auto;padding: 1em; }
					h1 { margin: 0; }
					small { font-style: italic; }
					h2 { border-bottom: 4px solid gray; }
					fieldset { margin: 1em 0; }
					legend { font-weight: bold;font-size: 1.3em; }
					h4 { margin: 0;font-style: italic; }
					h3, h5 { margin: 0;}
					h3 { margin-top: 1em; }
					p { margin-top: 0.2em;font-size: 0.8em; }
					pre { font-family: monospace; margin: 0;display: inline; }
					a[href='#'] { font-size: 0.6em; }
					ul { margin: 0.2em;padding-left: 1em; }
					li { font-size: 0.8em; }
					.warning { color: red;font-weight: bold;border: 1px solid orange;padding: 1em;margin: 1em 0; }
					.button { border: 1px solid gray;color: black;text-decoration: none;padding: .3em .5em;border-radius: 0.3em;background: #EEE;}
				</style>
			</head>
			<body>");
		}
	}
}
