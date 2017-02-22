using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	internal class HtmlHeadAndStyles : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write(@"<!DOCTYPE html>

			<html>
			<head>
				<title>Unicorn Control Panel</title>
				<link href='https://fonts.googleapis.com/css?family=Source+Sans+Pro:400,700,400italic' rel='stylesheet' type='text/css'>
				<style>
					* {
						font-family: 'Source Sans Pro', sans-serif;
					}
		
					/* vertical rhythms */
					article { margin: 30px 0; }
					article > * { margin: 20px 0 0 0; }
					td > *, 
					section > * { margin: 0 0 8px 0; }

					/* general styles */
					body {
						max-width: 960px;
						margin: 0 auto;
						padding: 1em;
					}

					code {
						font-family: monospace;
					}

					h2 {
						font-size: 2.2rem;
						color: #E35131;
					}

					h3 {
						font-size: 1.7rem;

					}

					h4 {
						font-size: 1.4rem;
					}

					input {
						color: white;
						border: 2px solid #E35131;
						display: block;
					}

					input[type=checkbox] {
						float: left;
						margin-top: 6px;
					}

					label {
						font-weight: bold;
					}

					li {
						list-style-type: none;
						margin: 0;
						padding: 0 0 8px 0;
					}

					select {
						border: 2px solid #E35131;
						color: #E35131;
						font-size: 1em;
						padding: 3px;
					}

					ul {
						margin: 0;
						padding: 0;
					}

					table {
						border-spacing: 0 20px;
						margin: 0 0 -20px 0;
						width: 100%;
					}

					td {
						padding: 0;
					}

					td + td {
						padding-left: 20px;
					}

					/* allows clicking when sidebar active */
					td > *, .verbosity-wrapper { position: relative; }

					/* specific styles */
					.badge {
						color: #FFF;
						font-weight: bold;
						background: rgba(227, 81, 49, .2);
						padding: 3px 8px;
					}
						.badge a { color: #FFF; }

					.batch {
						display: none;
						position: fixed;
						top: 400px;
						width: 100%;
						max-width: 960px;
					}

					.batch > section {
						float: right;
						background: white;
						width: 200px;
					}

					.batch a {
						display: block;
						text-align: center;
					}

					.button {
						display: inline-block;
						border: 2px solid #E35131;
						color: #E35131;
						text-decoration: none;
						padding: .3em .5em;
						font-size: 1.2rem;
						min-width: 85px;
						text-align: center;
					}

					.button:hover {
						background: #E35131;
						color: white;
					}

					.controls {
						white-space: nowrap;
						width: 200px;
						text-align: right;
						vertical-align: top;
						padding-top: 1.7rem;
					}

					.fakebox {
						font-weight: bold;
						cursor: pointer;
						position: relative;
					}

						.fakebox > span {
							width: 20px;
							height: 20px;
							border: 2px solid #E35131;
							font-size: 1rem;
							display: block;
							float: left;
							margin: .35em .4em 0 0;
						}

						h2.fakebox {
							width: calc(100% - 200px);
						}

						h2.fakebox > span {
							margin: .75em .4em 0 0;
						}

						.fakebox.checked > span:before {
							content: '✓';
							margin-left: 3px;
							color: #E35131;
						}

					.help {
						font-style: italic;
						color: #666;
						font-size: 0.9rem;
					}

					.hidden {
						display: none;
					}

					.overlay {
					  visibility: hidden;
					  opacity: 0;
					  position: fixed;
					  top: 0;
					  bottom: 0;
					  right: 0;
					  left: 0;
					  z-index: 2;
					  width: 100%;
					  height: 100%;
					  background-color: rgba(0,0,0,0.85);
					  cursor: pointer;
					  transition: opacity 0.3s ease-in-out;
					}

						.overlay h2 { margin-top: 0; }

						.overlay .modal {
						  position: absolute;
						  z-index: 3;
						  top: 0;
						  bottom: 0;
						  right: 0;
						  left: 0;
						  margin: auto;
						  min-width: 500px;
						  max-width: 80%;
						  max-height: 80%;
						  overflow: scroll;
						  padding: 20px;
						  background-color: #FFF;
						}

						.overlay.shown {
						  opacity: 1;
						}
	
					.syncall a, .syncall a:visited {
						color: #E35131;
						text-decoration: none;
						display: inline-block;
						line-height:110px;
						height: 100px;
					}

					.version {
						font-style: italic;
						font-size: 0.9rem;
					}

					.warning {
						color: white;
						font-weight: bold;
						background: #E35131;
						padding: 1em;
						margin: 1em 0;
					}

					/* IMAGE STYLES */
					svg {
						margin: 1em 0 2em 0;
						height: 200px;
						display: block;
					}

					svg.allthethings {
						margin: 0;
						height: 70px;
						display: inline-block;
						vertical-align: middle;
						padding: 0 0 18px 10px;
					}
				</style>
			</head>
			<body>");
		}
	}
}
