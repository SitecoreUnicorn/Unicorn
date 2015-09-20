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
				<link href='https://fonts.googleapis.com/css?family=Source+Sans+Pro:400,700,400italic' rel='stylesheet' type='text/css'>
				<style>
					* { font-family: 'Source Sans Pro', sans-serif; }
					body { max-width: 960px; margin: 0 auto;padding: 1em; }
					h1 { margin: 0; }
					small { font-style: italic; }
					svg { margin: 1em 0 2em 0; height: 200px; display: block; }
					h2 { border-bottom: 4px solid black; font-size: 2.2rem; line-height: 80%; margin: .5em 0; }
					h4 { margin: .5em 0 0 0; font-size: 1.4rem; }
					p { margin-top: 0.2em; }
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
						top: -1.7rem;
						font-size: 1.7rem;
						background: white;
					}
					.configuration h3 + * {
						margin-top: -1.5rem;
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
					.details h5 {
						display: inline-block;
						margin: 0 0 0 -5px;
						font-size: 1.3rem;
						position: relative;
						background: white;
						padding: 0 5px;
						top: -1.3rem;
					}
					h4.expand {
						color: blue;
						text-decoration: underline;
						cursor: pointer;
					}
					h4.expand::before {
						content: '+ ';
					}
					.warning { color: red;font-weight: bold;border: 1px solid orange;padding: 1em;margin: 1em 0; }
					.strong-info { color: orange; font-weight: bold; }
					.button {
						display: inline-block;
						border: 2px solid #E35131;
						color: #E35131;
						text-decoration: none;
						padding: .3em .5em;
						margin: 0 0 1em 0;
						font-size: 1.2rem;
					}
					.button:hover {
						background: #E35131;
						color: white;
					}
					.collapsed { display: none; }
				</style>
			</head>
			<body>");
		}
	}
}
