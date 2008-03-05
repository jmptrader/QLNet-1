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

namespace QLNet
{
   // Base class for cash flows
   // This class is purely virtual and acts as a base class for the
   // actual cash flow implementations.
   //    
	public abstract class CashFlow : Event
	{
      /// <summary>
      /// CashFlow interface
      /// returns the amount of the cash flow
      /// <remarks>
      /// The amount is not discounted, i.e., it is the actual
      /// amount paid at the cash flow date.
      /// </remarks> 
      /// </summary>
      /// <returns></returns>
      public virtual double amount()
      {
         throw new Exception("CashFlow.amount : the method or operation is not implemented.");
      }
      /// <summary>
      /// Event interface
      /// <remarks>
      /// This is inheriited from the event class
      /// </remarks> 
      /// </summary>
      /// <returns></returns>
      public abstract override DDate date() ;
      /// <summary>
      /// Visitability
      /// </summary>
      /// <param name="av"></param>
      /// 
      public virtual void accept(ref AcyclicVisitor v)
      {
         //Visitor<CashFlow> v1 = v as Visitor<CashFlow>;
         //if (v1 != null)
         //   v1.visit(ref this);
         //else
         //   Event.accept(ref v);
      }

	}

}