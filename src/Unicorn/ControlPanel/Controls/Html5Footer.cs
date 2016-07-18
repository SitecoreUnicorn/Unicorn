using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	internal class Html5Footer : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			// this allows expanding the dependency details of a configuration when it has serialized items already
			// yes, jQuery is total overkill. yes, deal with it. :)
			writer.Write("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.12.0/jquery.min.js\"></script>");
			writer.Write("<script src=\"https://cdnjs.cloudflare.com/ajax/libs/js-cookie/2.1.0/js.cookie.min.js\"></script>");
			writer.Write(@"<script>
		/* Overlays */
		(function($) { 
			$.fn.overlay = function() {
				overlay = $(this);
				overlay.ready(function() {
					overlay.on('transitionend webkitTransitionEnd oTransitionEnd MSTransitionEnd', function(e) {
						if (!$(this).hasClass('shown')) return $(this).css('visibility', 'hidden');
					});
					overlay.on('show', function() {
						var $this = $(this);
						$this.css('visibility', 'visible');
						$this.addClass('shown');
						return true;
					});
					overlay.on('hide', function() {
						$(this).removeClass('shown');
						return true;
					});
					overlay.on('click', function(e) {
						if (e.target.className === $(this).attr('class')) return $(this).trigger('hide');
					})
					$('a[data-overlay-trigger=""]').on('click', function() {
						overlay.trigger('show');
					});
					$('a[data-modal]:not([data-modal=""])').on('click', function(e) {
						$('#' + $(this).data('modal')).trigger('show');

						e.preventDefault();
					});
				})
			};
		})(jQuery);

		jQuery(function() {
			$('.overlay').overlay();
		});

		/* Multiple Selection */
		$(function() {
			$('.fakebox:not(.fakebox-all)').on('click', function() {
				var $this = $(this);

				$this.toggleClass('checked');

				if(!$this.hasClass('checked')) $('.fakebox-all').removeClass('checked');

				UpdateBatch();
			});

			$('.fakebox-all').on('click', function() {
				var $this = $(this);

				$this.toggleClass('checked');

				var $fakeboxes = $('.fakebox:not(.fakebox-all)');

				if($this.hasClass('checked')) $fakeboxes.addClass('checked');
				else $fakeboxes.removeClass('checked');

				UpdateBatch();
			});
		});

		function UpdateBatch() {
			var $fakeboxes = $('.fakebox:not(.fakebox-all)');
			var checked = $fakeboxes.filter('.checked')
				.map(function() { return $(this).text().trim(); })
				.get();

			var allSelected = checked.length == $fakeboxes.length;
			var configSpec = checked.join('^');
			var verbosity = $('#verbosity').val();

			$('.batch-sync').attr('href', '?verb=Sync&configuration=' + configSpec + '&log=' + verbosity);
			$('.batch-reserialize').attr('href', '?verb=Reserialize&configuration=' + configSpec + '&log=' + verbosity);
			$('.batch-configurations').html('<li>' + (allSelected ? 'All Configurations' :checked.join('</li><li>')) + '</li>');
			if(allSelected) $('.fakebox-all').addClass('checked');

			if(checked.length > 0) {
				$('.batch').finish().slideDown();
				$('td + td').css('visibility', 'hidden');
			}
			else {
				$('.batch').finish().slideUp(function() {
					$('td + td').css('visibility', 'visible');
				});	
			}
		}

		/* Verbosity */
		$(function() {
			var verbosityCookie = Cookies.get('UnicornLogVerbosity');
			if(verbosityCookie) {
				$('#verbosity').val(verbosityCookie);
			}

			UpdateVerbosity();

			$('#verbosity').on('change', function() {
				UpdateBatch();
				UpdateVerbosity();
			});
		});

		function UpdateVerbosity() {
			var verbosity = $('#verbosity').val();

			$('[data-basehref]').each(function() {
				$(this).attr('href', $(this).data('basehref') + '&log=' + verbosity);
			});

			Cookies.set('UnicornLogVerbosity', verbosity, { expires: 730 });
		}
	</script>");
			writer.Write(" </body></html>");
		}
	}
}
