/*
 Copyright (C) 2008 Andrea Maggiulli
  
 This file is part of QLNet Project http://trac2.assembla.com/QLNet

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
using System.Text;

namespace QLNet.Global
{
   public static class Math
   {
      public static bool close(double x, double  y)
      {
         return close(x, y, 42);
      }

      public static bool close(double x, double y, int n)
      {
         double diff = System.Math.Abs(x - y);
         double tolerance = n * Double.Epsilon;
         // FLOATING_POINT_EXCEPTION
         return diff <= tolerance * System.Math.Abs(x) && diff <= tolerance * System.Math.Abs(y);
      }

   }
}
