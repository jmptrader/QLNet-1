/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
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
using System.Threading;

namespace QLNet {
    //! global repository for past index fixings
    public static class IndexManager {

        [ThreadStatic]
        private static Dictionary<string, TimeSeries<double>> data_ = null;
        public static Dictionary<string, TimeSeries<double>> Data
        {
            get
            {
                if (data_ == null)
                {
                    data_ = new Dictionary<string, TimeSeries<double>>();
                }
                return data_;
            }
        }

		//! returns whether historical fixings were stored for the index
        public static bool hasHistory(string name) {
			return Data.ContainsKey(name);
		}
		
        //! returns the (possibly empty) history of the index fixings
        public static TimeSeries<double> getHistory(string name) {
            return hasHistory(name) ? Data[name] : new TimeSeries<double>();
		}
		
        //! stores the historical fixings of the index
        public static void setHistory(string name, TimeSeries<double> history) {
            if (hasHistory(name))
                Data[name] = history;
            else
                Data.Add(name, history);
        }

        //! observer notifying of changes in the index fixings
        public static TimeSeries<double> notifier(string name) {
            return Data[name];
        }

        //! returns all names of the indexes for which fixings were stored
        public static List<string> histories() {
            List<string> t = new List<string>();
            foreach (string s in Data.Keys)
                t.Add(s);
	        return t;
	    }
	
        //! clears the historical fixings of the index
        public static void clearHistory(string name) {
			Data[name].Clear();
		}

        //! clears all stored fixings
        public static void clearHistories() {
			Data.Clear();
		}
	}
}