using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EhlersSmoothedAdaptiveMomentum : Indicator
    {
        private IndicatorDataSeries _s, _c, _q1, _ip, _i1, _dp, _p;

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Alpha", DefaultValue = 0.07)]
        public double Alpha { get; set; }

        [Parameter("Cut Off", DefaultValue = 8)]
        public double CutOff { get; set; }

        [Output("SAM", LineColor = "Black", PlotType = PlotType.Line)]
        public IndicatorDataSeries Sam { get; set; }

        [Output("Positive", LineColor = "Green", PlotType = PlotType.Histogram)]
        public IndicatorDataSeries Positive { get; set; }

        [Output("Negative", LineColor = "Red", PlotType = PlotType.Histogram)]
        public IndicatorDataSeries Negative { get; set; }

        protected override void Initialize()
        {
            _s = CreateDataSeries();
            _c = CreateDataSeries();
            _q1 = CreateDataSeries();
            _ip = CreateDataSeries();
            _dp = CreateDataSeries();
            _p = CreateDataSeries();
            _i1 = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            _s[index] = (Source[index] + 2 * Source[index - 1] + 2 * Source[index - 2] + Source[index - 3]) / 6;

            _c[index] = GetValueOrDefault(((1 - 0.5 * Alpha) * (1 - 0.5 * Alpha) * (_s[index] - 2 * GetValueOrDefault(_s[index - 1]) + GetValueOrDefault(_s[index - 2])) + 2 * (1 - Alpha) * GetValueOrDefault(_c[index - 1]) - (1 - Alpha) * (1 - Alpha) * GetValueOrDefault(_c[index - 2])),
                (_s[index] - 2 * _s[index - 1] + _s[index - 2]) / 4.0);

            _q1[index] = (.0962 * _c[index] + 0.5769 * GetValueOrDefault(_c[index - 2]) - 0.5769 * GetValueOrDefault(_c[index - 4]) - .0962 * GetValueOrDefault(_c[index - 6])) * (0.5 + .08 * GetValueOrDefault(_ip[index - 1]));

            _i1[index] = GetValueOrDefault(_c[index - 3]);

            var dp = _q1[index] != 0 && _q1[index - 1] != 0
                ? (_i1[index] / _q1[index] - GetValueOrDefault(_i1[index - 1]) / GetValueOrDefault(_q1[index - 1])) / (1 + _i1[index] * GetValueOrDefault(_i1[index - 1]) / (_q1[index] * GetValueOrDefault(_q1[index - 1])))
                : 0;

            if (dp < 0.1)
            {
                _dp[index] = 0.1;
            }
            else
            {
                _dp[index] = dp > 1.1 ? 1.1 : dp;
            }

            var md = Med(_dp[index], _dp[index - 1], Med(_dp[index - 2], _dp[index - 3], _dp[index - 4]));

            var dc = md == 0 ? 15 : 2 * Math.PI / md + 0.5;

            _ip[index] = .33 * dc + .67 * GetValueOrDefault(_ip[index - 1]);

            _p[index] = .15 * _ip[index] + .85 * GetValueOrDefault(_p[index - 1]);

            var pr = Math.Round(Math.Abs(_p[index] - 1));

            double vx = 0;

            for (int i = 1; i <= pr; i++)
            {
                vx += _s[index] - _s[index - i];
            }

            var a1 = Math.Exp(-Math.PI / CutOff);
            var b1 = 2.0 * a1 * Math.Cos((1.738 * 180 / CutOff) * (Math.PI / 180));
            var c1 = a1 * a1;
            var coef2 = b1 + c1;
            var coef3 = -(c1 + b1 * c1);
            var coef4 = c1 * c1;
            var coef1 = 1 - coef2 - coef3 - coef4;

            Sam[index] = GetValueOrDefault(coef1 * vx + coef2 * GetValueOrDefault(Sam[index - 1]) + coef3 * GetValueOrDefault(Sam[index - 2]) + coef4 * GetValueOrDefault(Sam[index - 3]), vx);

            Positive[index] = double.NaN;
            Negative[index] = double.NaN;

            if (Sam[index] > 0)
            {
                Positive[index] = Sam[index];
            }
            else
            {
                Negative[index] = Sam[index];
            }
        }

        private double GetValueOrDefault(double value, double defaultValue = 0)
        {
            return double.IsNaN(value) ? defaultValue : value;
        }

        private double Med(double x, double y, double z)
        {
            return (x + y + z) - Math.Min(x, Math.Min(y, z)) - Math.Max(x, Math.Max(y, z));
        }
    }
}