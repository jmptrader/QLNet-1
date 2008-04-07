/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet {

    //! bootstrap error
    public class BootstrapError : ISolver1d {
        private InterpolatedYieldCurve<Linear, IterativeBootstrap> curve_;
        private BootstrapHelper<YieldTermStructure> helper_;
        private int segment_;

        public BootstrapError(YieldTermStructure curve, BootstrapHelper<YieldTermStructure> helper, int segment) {
            curve_ = (InterpolatedYieldCurve<Linear, IterativeBootstrap>)curve;
            helper_ = helper;
            segment_ = segment; 
        }

        public override double value(double guess) {
            curve_.updateGuess(curve_.data(), guess, segment_);
            curve_.interpolation_.update();
            return helper_.quoteError();
        }
    }
}