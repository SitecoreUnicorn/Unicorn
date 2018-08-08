using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	internal class HtmlFooter : IControlPanelControl
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
			var skipTransparent = $('#skipTransparent').prop('checked') ? 1 : 0;

			$('.batch-sync').attr('href', '?verb=Sync&configuration=' + configSpec + '&log=' + verbosity + '&skipTransparentConfigs=' + skipTransparent);
			$('.batch-reserialize').attr('href', '?verb=Reserialize&configuration=' + configSpec + '&log=' + verbosity + '&skipTransparentConfigs=' + skipTransparent);
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

		$(function() {
			/* Verbosity */
			var verbosityCookie = Cookies.get('UnicornLogVerbosity');
			if(verbosityCookie) {
				$('#verbosity').val(verbosityCookie);
			}

			$('#verbosity').on('change', function() {
				UpdateBatch();
				UpdateOptions();
			});

			/* Transparent Skipping */
			var transparentCookie = Cookies.get('UnicornSkipTransparent');
			$('#skipTransparent').prop('checked', transparentCookie == '1' ? true : false);

			$('#skipTransparent').on('change', function() {
				UpdateBatch();
				UpdateOptions();
			});

			UpdateOptions();

		});

		function UpdateOptions() {
			var verbosity = $('#verbosity').val();
			var skipTransparent = $('#skipTransparent').prop('checked');

			$('[data-basehref]').each(function() {
				$(this).attr('href', $(this).data('basehref') + '&log=' + verbosity + '&skipTransparentConfigs=' + skipTransparent);
			});

			Cookies.set('UnicornSkipTransparent', skipTransparent ? 1 : 0, { expires: 730 });
			Cookies.set('UnicornLogVerbosity', verbosity, { expires: 730 });
		}

        var fakeboxAll = $('.fakebox-all');
		if (fakeboxAll.offset() != null) {
        var sticky = $('.batch');
        stickyTop = fakeboxAll.offset().top - fakeboxAll.height();
        $(window).scroll(function () {
            var scroll = $(window).scrollTop();

            if (scroll >= fakeboxAll.offset().top) {
                sticky.css({ 'top': 10 });
            }
            else {
                sticky.css({ 'top': stickyTop - scroll });
            }
        });};
	</script>");
			writer.Write(" </body></html>");
		}
	}
}
