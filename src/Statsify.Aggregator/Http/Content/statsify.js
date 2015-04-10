(function (statsify, undefined) {

  var endpointUrl = 'http://localhost:8080/statsify';
  var graphics = [];

  statsify.init = function(settings) {
    debugger;
    endpointUrl = settings.endpointUrl || endpointUrl;
  };

  statsify.data_graphic = function(target, expression, from, until, options) {

    var expressions = [];
    var i = 0;

    if (expression instanceof Array) {
      for (i = 0; i < expression.length; ++i) {
        expressions.push(expression[i]);
      }
    } else {
      expressions.push(expression);
    }

    var qs = [];

    for (i = 0; i < expressions.length; ++i) {
      qs.push('expression=' + encodeURIComponent(encodeURIComponent(expressions[i])));
    }

    if (from) qs.push('from=' + encodeURIComponent(from));
    if (until) qs.push('until=' + encodeURIComponent(until));

    var url = endpointUrl + '/api/v1/series?' + qs.join('&');

    graphics.push({ target: target, url: url, options: options });

    refresh_statsify_data_graphic(target, url, options);

  };

  function refresh_statsify_data_graphic(target, url, options) {

    d3.json(url,
      function (data) {

        var series = [];
        var legend = [];

        for (var i = 0; i < data.length; ++i) {
          var serie = [];
          legend.push(data[i].target);

          for (var j = 0; j < data[i].datapoints.length; ++j)
            serie.push({ x: new Date(1000 * data[i].datapoints[j][1]), y: data[i].datapoints[j][0] || 0 });

          series.push(serie);
        }

        var _options = {
          data: series,
          legend: legend,
          width: 1100,
          height: 250,
          right: 30,
          target: target,
          transition_on_update: false,
          x_accessor: 'x',
          y_accessor: 'y',
          rollover_callback: function (d, i) {
            var df = d3.time.format("%b %d, %H:%M");
            var html = legend[d.line_id - 1] + ': ' + df(d.x) + ' - ' + d3.format(',.2s')(d.y);

            $(target + ' svg .active_datapoint').html(html);
          }
        };

        var opts = $.extend({}, _options, options);

        MG.data_graphic(opts);
      });

  };

  function refresh_statsify_data_graphics() {

    for (var i = graphics.length - 1; i >= 0; i--) {
      refresh_statsify_data_graphic(graphics[i].target, graphics[i].url, graphics[i].options);
    };

    setTimeout(refresh_statsify_data_graphics, 5000);

  };

}(window.statsify = window.statsify || {}));