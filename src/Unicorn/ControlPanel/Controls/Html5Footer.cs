using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	internal class Html5Footer : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			// this allows expanding the dependency details of a configuration when it has serialized items already
			// yes, jQuery is total overkill. yes, deal with it. :)
			writer.Write("<script src=\"//ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js\"></script>");
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
					$('a[data-modal]:not([data-modal=""])').on('click', function() {
						$('#' + $(this).data('modal')).trigger('show');
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

			function UpdateBatch() {
				var $fakeboxes = $('.fakebox:not(.fakebox-all)');
				var checked = $fakeboxes.filter('.checked')
					.map(function() { return $(this).text().trim(); })
					.get();

				var allSelected = checked.length == $fakeboxes.length;
				var configSpec = checked.join('^');

				$('.batch-sync').attr('href', '?verb=Sync&configuration=' + configSpec);
				$('.batch-reserialize').attr('href', '?verb=Reserialize&configuration=' + configSpec);
				$('.batch-configurations').html('<li>' + (allSelected ? 'All Configurations' :checked.join('</li><li>')) + '</li>');
				if(allSelected) $('.fakebox-all').addClass('checked');

				if(checked.length > 0) {
					$('.batch').slideDown();
					$('td + td').css('visibility', 'hidden');
				}
				else {
					$('.batch').slideUp(function() {
						$('td + td').css('visibility', 'visible');
					});	
				}
			}
		});
	</script>");
			writer.Write(" </body></html>");
		}
	}
}
