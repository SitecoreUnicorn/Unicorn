using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class Html5Footer : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			// this allows expanding the dependency details of a configuration when it has serialized items already
			// yes, jQuery is total overkill. yes, deal with it. :)
			writer.Write("<script src=\"//ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js\"></script>");
			writer.Write(@"<script>
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
	</script>");
			writer.Write(" </body></html>");
		}
	}
}
