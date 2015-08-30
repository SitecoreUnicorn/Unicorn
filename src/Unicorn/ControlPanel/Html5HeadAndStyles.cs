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
				<link href='https://fonts.googleapis.com/css?family=Lobster|Open+Sans:400italic,400,800' rel='stylesheet' type='text/css'>
				<style>
					* { font-family: 'Open Sans', sans-serif; }
					body { max-width: 960px; margin: 0 auto;padding: 1em; }
					h1 { margin: 0; }
					small { font-style: italic; }
					svg { margin: 1em 0 2em 0; height: 200px; display: block; }
					h2 { border-bottom: 4px solid gray; font-family: Lobster, sans-serif; font-size: 2.2em; line-height: 90%; font-weight: normal; margin: .5em 0; }
					h4 { margin: .5em 0 0 0; }
					p { margin-top: 0.2em; font-size: 0.8rem; }
					code { font-family: monospace; }
					a[href='#'] { font-size: 0.7em; }
					ul { margin: 0; padding: 0; }
					li { list-style-type: none; margin: 0; padding: 0; }
					.configuration {
						border: 1px solid gray;
						padding: 10px;
						margin-top: 2em;
					}
					.configuration h3 {
						display: inline-block;
						position: relative;
						margin: 0 0 0 -5px;
						padding: 0 5px;
						top: -20px;
						background: white;
					}
					.configuration h3 + section {
						margin-top: -15px;
					}
					.details > li {
						border-top: 1px solid #DDD;
						border-left: 1px solid #DDD;
						margin: 20px 0;
						padding:  10px;
					}
					.details h5 + p {
						margin-top: -10px;
					}
					.details ul > li {
						font-size: 0.7em;
					}
					.details h5 {
						display: inline-block;
						margin: 0 0 0 -5px;
						font-size: 1em;
						position: relative;
						background: white;
						padding: 0 5px;
						top: -20px;
					}
					h4.expand {
						color: blue;
						text-decoration: underline;
						cursor: hand;
					}
					h4.expand::before {
						content: '+ ';
					}
					.warning { color: red;font-weight: bold;border: 1px solid orange;padding: 1em;margin: 1em 0; }
					.button {
						display: inline-block;
						border: 1px solid gray;
						color: black;
						text-decoration: none;
						padding: .3em .5em;
						border-radius: 0.3em;
						background: #EEE;
						margin: 0 0 1em 0;
					}
					.collapsed { display: none; }
				</style>
			</head>
			<body>");
		}
	}
}
