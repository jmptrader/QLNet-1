﻿/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)  
  
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

using QLNet.Time;

namespace QLNet
{
	/// <summary>
	/// principal payment over a fixed period
	/// 
	/// This class implements part of the CashFlow interface but it is
	/// still abstract and provides derived classes with methods for accrual period calculations.
	/// </summary>
	public class Principal : CashFlow
	{
		protected double nominal_;
		protected double amount_;
		protected DayCounter dayCounter_;
		protected Date paymentDate_, accrualStartDate_, accrualEndDate_, refPeriodStart_, refPeriodEnd_;

		// access to properties
		public double nominal() { return nominal_; }

		public override Date Date
		{
			get { return paymentDate_; }
		}

		public Date accrualStartDate() { return accrualStartDate_; }

		public Date accrualEndDate() { return accrualEndDate_; }

		public Date refPeriodStart { get { return refPeriodStart_; } }

		public Date refPeriodEnd { get { return refPeriodEnd_; } }

		public override double amount() { return amount_; }

		public void setAmount(double amount) { amount_ = amount; }

		public DayCounter dayCounter() { return dayCounter_; }

		public Principal()
		{
		}

		public Principal(double amount, double nominal, Date paymentDate, Date accrualStartDate, Date accrualEndDate, DayCounter dayCounter)
			: this(amount, nominal, paymentDate, accrualStartDate, accrualEndDate, dayCounter, null, null)
		{
		}

		public Principal(double amount, double nominal, Date paymentDate, Date accrualStartDate, Date accrualEndDate, DayCounter dayCounter, Date refPeriodStart, Date refPeriodEnd)
		{
			amount_ = amount;
			nominal_ = nominal;
			paymentDate_ = paymentDate;
			accrualStartDate_ = accrualStartDate;
			accrualEndDate_ = accrualEndDate;
			dayCounter_ = dayCounter;
			refPeriodStart_ = refPeriodStart ?? accrualStartDate_;
			refPeriodEnd_ = refPeriodEnd ?? accrualEndDate_;
		}
	}
}
