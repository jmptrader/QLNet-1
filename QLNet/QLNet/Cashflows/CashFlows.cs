/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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
using System.Reflection;

namespace QLNet 
{
   //! %cashflow-analysis functions
   public static class CashFlows 
   {
      const double basisPoint_ = 1.0e-4;

      
      #region utility functions
      
      private static double couponRate(List<CashFlow> leg, CashFlow cf) {
            if (cf == leg.Last()) return 0.0;

            Date paymentDate = cf.date();
            bool firstCouponFound = false;
            double nominal = 0;
            double accrualPeriod = 0;
            DayCounter dc = null;
            double result = 0.0;

            foreach (CashFlow x in leg.Where(x => x.date() == paymentDate)) {
                Coupon cp = x as Coupon;
                if (cp != null) {
                    if (firstCouponFound) {
                        if (!(nominal == cp.nominal() && accrualPeriod == cp.accrualPeriod() && dc == cp.dayCounter()))
                            throw new ApplicationException("cannot aggregate two different coupons on " + paymentDate);
                    } else {
                        firstCouponFound = true;
                        nominal = cp.nominal();
                        accrualPeriod = cp.accrualPeriod();
                        dc = cp.dayCounter();
                    }
                    result += cp.rate();
                }
            }

            if (!firstCouponFound) throw new ApplicationException("next cashflow (" + paymentDate + ") is not a coupon");
            return result;
        }
      public static double simpleDuration(List<CashFlow> cashflows, InterestRate y, Date settlementDate) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         double P = 0, dPdy = 0;
         DayCounter dc = y.dayCounter();
         foreach (CashFlow cf in cashflows.Where(cf => !cf.hasOccurred(settlementDate))) 
         {
            double t = dc.yearFraction(settlementDate, cf.date());
            double c = cf.amount();
            double B = y.discountFactor(t);
            P += c * B;
            dPdy += t * c * B;
         }


         // no cashflows
         if (P == 0.0) return 0.0;
         return dPdy / P;
      }
      public static double modifiedDuration(List<CashFlow> cashflows, InterestRate y, Date settlementDate) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         double P = 0.0;
         double dPdy = 0.0;
         double r = y.rate();
         int N = (int)y.frequency();
         DayCounter dc = y.dayCounter();

         foreach (CashFlow cf in cashflows.Where(cf => !cf.hasOccurred(settlementDate))) 
         {

            double t = dc.yearFraction(settlementDate, cf.date());
            double c = cf.amount();
            double B = y.discountFactor(t);

            P += c * B;

            switch (y.compounding()) 
            {
               case Compounding.Simple:
                  dPdy -= c * B * B * t;
                  break;
                  
               case Compounding.Compounded:
                  dPdy -= c * t * B / (1 + r / N);
                  break;
                  
               case Compounding.Continuous:
                  dPdy -= c * B * t;
                  break;
                  
               case Compounding.SimpleThenCompounded:
                  if (t <= 1.0 / N)
                     dPdy -= c * B * B * t;
                  else
                     dPdy -= c * t * B / (1 + r / N);
                     break;
                    
               default:
                  throw new ArgumentException("unknown compounding convention (" + y.compounding() + ")");
            }
         }
         
         if (P == 0.0) // no cashflows
            return 0.0;
            
         return -dPdy / P; // reverse derivative sign
      }
      public static double macaulayDuration(List<CashFlow> cashflows, InterestRate y, Date settlementDate) 
      {
         if (y.compounding() != Compounding.Compounded) throw new ArgumentException("compounded rate required");
         return (1.0 + y.rate() / (int)y.frequency()) * modifiedDuration(cashflows, y, settlementDate);
      }
      
      #endregion

      #region helper classes
        class IrrFinder : ISolver1d {
            List<CashFlow> cashflows_;
            double marketPrice_;
            DayCounter dayCounter_;
            Compounding compounding_;
            Frequency frequency_;
            Date settlementDate_;

            public IrrFinder(List<CashFlow> cashflows, double marketPrice, DayCounter dayCounter,
                             Compounding compounding, Frequency frequency, Date settlementDate) {
                cashflows_ = cashflows;
                marketPrice_ = marketPrice;
                dayCounter_ = dayCounter;
                compounding_ = compounding;
                frequency_ = frequency;
                settlementDate_ = settlementDate;
            }

            public override double value(double x) {
                InterestRate y = new InterestRate(x, dayCounter_, compounding_, frequency_);
                double NPV = CashFlows.npv(cashflows_, y, settlementDate_);
                return marketPrice_ - NPV;
            }
            public override double derivative(double x) {
                InterestRate y = new InterestRate(x, dayCounter_, compounding_, frequency_);
                return modifiedDuration(cashflows_, y, settlementDate_);
            }
        };


        class BPSCalculator : IAcyclicVisitor {
            private YieldTermStructure termStructure_;
            private Date npvDate_;
            private double result_;

            public BPSCalculator(YieldTermStructure termStructure, Date npvDate) {
                termStructure_ = termStructure;
                npvDate_ = npvDate;
                result_ = 0;
            }

            #region IAcyclicVisitor pattern
            // visitor classes should implement the generic visit method in the following form
            public void visit(object o) {
                Type[] types = new Type[] { o.GetType() };
                MethodInfo methodInfo = this.GetType().GetMethod("visit", types);
                if (methodInfo != null) {
                    methodInfo.Invoke(this, new object[] { o });
                }
            }
            public void visit(Coupon c) {
                result_ += c.accrualPeriod() * c.nominal() * termStructure_.discount(c.date());
            }
            #endregion

            public double result() {
                if (npvDate_ == null) return result_;
                else return result_ / termStructure_.discount(npvDate_);
            }
        }
        #endregion

      //! \name Date functions
      //@{
      public static Date startDate(List<CashFlow> cashflows)
      {
         if (cashflows.Count == 0) throw new ArgumentException("no cashflows");
         return cashflows.Where(cf => cf is Coupon).Min(cf => ((Coupon)cf).accrualStartDate());
      }
      public static Date maturityDate(List<CashFlow> cashflows)
      {
         if (cashflows.Count == 0) throw new ArgumentException("no cashflows");
         return cashflows.Max(c => c.date());
      }
      public static bool isExpired(List<CashFlow> cashflows, bool includeSettlementDateFlows)
      { return isExpired(cashflows,includeSettlementDateFlows, null); }
      public static bool isExpired(List<CashFlow> cashflows, bool includeSettlementDateFlows,
                                     Date settlementDate)
      {
         if (cashflows.Count == 0)
            return true;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         for (int i = cashflows.Count; i > 0; --i)
            if (!cashflows[i - 1].hasOccurred(settlementDate,includeSettlementDateFlows))
               return false;
         return true;
      }
      //@}

      public static CashFlow previousCashFlow(List<CashFlow> leg) { return previousCashFlow(leg, null); }
      public static CashFlow previousCashFlow(List<CashFlow> leg, Date refDate) {
            Date d = (refDate == null ? Settings.evaluationDate() : refDate);

            CashFlow cf = leg.FindLast(x => x.hasOccurred(d));
            if (cf == null) return leg.Last();
            else return cf;
        }

      public static CashFlow nextCashFlow(List<CashFlow> leg) { return nextCashFlow(leg, null); }
      public static CashFlow nextCashFlow(List<CashFlow> leg, Date refDate) {
            Date d = (refDate == null ? Settings.evaluationDate() : refDate);

            // the first coupon paying after d is the one we're after
            CashFlow cf = leg.Find(x => !x.hasOccurred(d));
            if (cf == null) return leg.Last();
            else return cf;
        }

      public static double previousCouponRate(List<CashFlow> leg) { return previousCouponRate(leg, null); }
      public static double previousCouponRate(List<CashFlow> leg, Date refDate) {
            CashFlow cf = previousCashFlow(leg, refDate);
            return couponRate(leg, cf);
        }

      public static double nextCouponRate(List<CashFlow> leg) { return nextCouponRate(leg, null); }
      public static double nextCouponRate(List<CashFlow> leg, Date refDate) {
            CashFlow cf = nextCashFlow(leg, refDate);
            return couponRate(leg, cf);
        }

      public static Date previousCouponDate(List<CashFlow> leg, Date refDate) {
            var cf = previousCashFlow(leg, refDate);
            if (cf==leg.Last()) return null;
            return cf.date();
        }

      public static Date nextCouponDate(List<CashFlow> leg, Date refDate) {
            var cf = nextCashFlow(leg, refDate);
            if (cf==leg.Last()) return null;
            return cf.date();
        }


      //! NPV of the cash flows. The NPV is the sum of the cash flows, each discounted according to the given term structure.
      public static double npv(List<CashFlow> cashflows, YieldTermStructure discountCurve) {
            return npv(cashflows, discountCurve, null, null, 0);
        }
      public static double npv(List<CashFlow> cashflows, YieldTermStructure discountCurve, Date settlementDate, Date npvDate) 
      {
         return npv(cashflows, discountCurve, settlementDate, npvDate, 0);
      }
        
      public static double npv(List<CashFlow> cashflows, YieldTermStructure discountCurve, 
                               Date settlementDate,Date npvDate, int exDividendDays) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         if (settlementDate == null)
            settlementDate = discountCurve.referenceDate();

         double totalNPV = cashflows.Where(x => !x.hasOccurred(settlementDate + exDividendDays)).
                                        Sum(c => c.amount() * discountCurve.discount(c.date()));

         if (npvDate == null) 
            return totalNPV;
         else 
            return totalNPV / discountCurve.discount(npvDate);
      }

      // NPV of the cash flows.
        // The NPV is the sum of the cash flows, each discounted according to the given constant interest rate.  The result
        // is affected by the choice of the interest-rate compounding and the relative frequency and day counter.
      public static double npv(List<CashFlow> cashflows, InterestRate r) { return npv(cashflows, r, null); }
      public static double npv(List<CashFlow> cashflows, InterestRate r, Date settlementDate) 
        {
        
           if (settlementDate == null)
              settlementDate = Settings.evaluationDate();

           FlatForward flatRate = new FlatForward(settlementDate, r.rate(), r.dayCounter(), r.compounding(), r.frequency());
           return npv(cashflows, flatRate, settlementDate, settlementDate);
        }

      //! CASH of the cash flows. The CASH is the sum of the cash flows.
      public static double cash(List<CashFlow> cashflows)
        {
           return cash(cashflows, null, 0);
        }
      public static double cash(List<CashFlow> cashflows, Date settlementDate)
        {
           return cash(cashflows, settlementDate, 0);
        }
      public static double cash(List<CashFlow> cashflows, Date settlementDate, int exDividendDays)
      {
         if (cashflows.Count == 0)
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         double totalCASH = cashflows.Where(x => !x.hasOccurred(settlementDate + exDividendDays)).
            Sum(c => c.amount());

         return totalCASH;
      }

      // Basis-point sensitivity of the cash flows.
      // The result is the change in NPV due to a uniform 1-basis-point change in the rate paid by the cash flows. The change for each coupon is discounted according to the given term structure.
      public static double bps(List<CashFlow> cashflows, YieldTermStructure discountCurve) 
      {
         return bps(cashflows, discountCurve, null, null, 0);
      }
      public static double bps(List<CashFlow> cashflows, YieldTermStructure discountCurve, 
                               Date settlementDate, Date npvDate) 
      {
         return bps(cashflows, discountCurve, settlementDate, npvDate, 0);
      }
        
      public static double bps(List<CashFlow> cashflows, YieldTermStructure discountCurve,
                               Date settlementDate, Date npvDate, int exDividendDays) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         if (settlementDate == null)
            settlementDate = discountCurve.referenceDate();

         BPSCalculator calc = new BPSCalculator(discountCurve, npvDate);
         for (int i = 0; i < cashflows.Count; i++)
            if (!cashflows[i].hasOccurred(settlementDate + exDividendDays))
               cashflows[i].accept(calc);

         return basisPoint_ * calc.result();
      }

      // Basis-point sensitivity of the cash flows.
        // The result is the change in NPV due to a uniform 1-basis-point change in the rate paid by the cash flows. The change for each coupon is discounted according
        //  to the given constant interest rate.  The result is affected by the choice of the interest-rate compounding and the relative frequency and day counter.
      public static double bps(List<CashFlow> cashflows, InterestRate irr) { return bps(cashflows, irr, null); }
      public static double bps(List<CashFlow> cashflows, InterestRate irr, Date settlementDate) {
            if (settlementDate == null)
                settlementDate = Settings.evaluationDate();
            var flatRate = new FlatForward(settlementDate, irr.rate(), irr.dayCounter(), irr.compounding(), irr.frequency());
            return bps(cashflows, flatRate, settlementDate, settlementDate);
        }

      // At-the-money rate of the cash flows.
        // The result is the fixed rate for which a fixed rate cash flow  vector, equivalent to the input vector, has the required NPV according to the given term structure. If the required NPV is
        //  not given, the input cash flow vector's NPV is used instead.
      public static double atmRate(List<CashFlow> cashflows, YieldTermStructure discountCurve) {
            return atmRate(cashflows, discountCurve, null, null, 0, default(double));
        }
      //public static double atmRate(List<CashFlow> cashflows, YieldTermStructure discountCurve, Date settlementDate) {
        //    return atmRate(cashflows, discountCurve, settlementDate, null, 0, default(double));
        //}
        //public static double atmRate(List<CashFlow> cashflows, YieldTermStructure discountCurve, Date settlementDate, Date npvDate) {
        //    return atmRate(cashflows, discountCurve, settlementDate, npvDate, 0, default(double));
        //}
        //public static double atmRate(List<CashFlow> cashflows, YieldTermStructure discountCurve, Date settlementDate, Date npvDate, int exDividendDays) {
        //    return atmRate(cashflows, discountCurve, settlementDate, npvDate, exDividendDays, default(double));
        //}
      public static double atmRate(List<CashFlow> cashflows, YieldTermStructure discountCurve,
                                      Date settlementDate, Date npvDate, int exDividendDays, double npv) {
            double bps = CashFlows.bps(cashflows, discountCurve, settlementDate, npvDate, exDividendDays);
            if (npv == default(double))
                npv = CashFlows.npv(cashflows, discountCurve, settlementDate, npvDate, exDividendDays);
            return basisPoint_ * npv / bps;
        }


      //! Internal rate of return.
      /*! The IRR is the interest rate at which the NPV of the cash flows equals the given market price. The function verifies
            the theoretical existance of an IRR and numerically establishes the IRR to the desired precision. */
      public static double irr(List<CashFlow> cashflows, double marketPrice, DayCounter dayCounter, Compounding compounding,
                              Frequency frequency, Date settlementDate, double accuracy, int maxIterations, double guess) {
            if (settlementDate == null)
                settlementDate = Settings.evaluationDate();

            // depending on the sign of the market price, check that cash flows of the opposite sign have been specified (otherwise
            // IRR is nonsensical.)

            int lastSign = Math.Sign(-marketPrice),
                signChanges = 0;

            foreach (CashFlow cf in cashflows.Where(cf => !cf.hasOccurred(settlementDate))) {
                int thisSign = Math.Sign(cf.amount());
                if (lastSign * thisSign < 0) // sign change
                    signChanges++;

                if (thisSign != 0)
                    lastSign = thisSign;
            }
            if (!(signChanges > 0))
                throw new ApplicationException("the given cash flows cannot result in the given market price due to their sign");

            /* The following is commented out due to the lack of a QL_WARN macro
            if (signChanges > 1) {    // Danger of non-unique solution
                                      // Check the aggregate cash flows (Norstrom)
                Real aggregateCashFlow = marketPrice;
                signChanges = 0;
                for (Size i = 0; i < cashflows.size(); ++i) {
                    Real nextAggregateCashFlow =
                        aggregateCashFlow + cashflows[i]->amount();

                    if (aggregateCashFlow * nextAggregateCashFlow < 0.0)
                        signChanges++;

                    aggregateCashFlow = nextAggregateCashFlow;
                }
                if (signChanges > 1)
                    QL_WARN( "danger of non-unique solution");
            };
            */

            //Brent solver;
            NewtonSafe solver = new NewtonSafe();
            solver.setMaxEvaluations(maxIterations);
            return solver.solve(new IrrFinder(cashflows, marketPrice, dayCounter, compounding, frequency, settlementDate),
                                accuracy, guess, guess / 10.0);
        }

      //! Cash-flow duration.
      /*! The simple duration of a string of cash flows is defined as
                \f[
                D_{\mathrm{simple}} = \frac{\sum t_i c_i B(t_i)}{\sum c_i B(t_i)}
                \f]
                where \f$ c_i \f$ is the amount of the \f$ i \f$-th cash
                flow, \f$ t_i \f$ is its payment time, and \f$ B(t_i) \f$ is the corresponding discount according to the passed yield.

                The modified duration is defined as
                \f[
                D_{\mathrm{modified}} = -\frac{1}{P} \frac{\partial P}{\partial y}
                \f]
                where \f$ P \f$ is the present value of the cash flows according to the given IRR \f$ y \f$.

                The Macaulay duration is defined for a compounded IRR as
                \f[
                D_{\mathrm{Macaulay}} = \left( 1 + \frac{y}{N} \right)
                                        D_{\mathrm{modified}}
                \f]
                where \f$ y \f$ is the IRR and \f$ N \f$ is the number of cash flows per year.
            */
      public static double duration(List<CashFlow> cashflows, InterestRate rate) { return duration(cashflows, rate, Duration.Type.Modified, null); }
      public static double duration(List<CashFlow> cashflows, InterestRate rate, Duration.Type type) { return duration(cashflows, rate, type, null); }
      public static double duration(List<CashFlow> cashflows, InterestRate rate, Duration.Type type, Date settlementDate) 
        {

           if (cashflows.Count == 0)
              return 0.0;

           if (settlementDate == null) settlementDate = Settings.evaluationDate();
           
           switch (type) 
           {
              case Duration.Type.Simple:
                 return simpleDuration(cashflows, rate, settlementDate);
                
              case Duration.Type.Modified:
                 return modifiedDuration(cashflows, rate, settlementDate);
                
              case Duration.Type.Macaulay:
                 return macaulayDuration(cashflows, rate, settlementDate);
                
              default:
                 throw new ArgumentException("unknown duration type");
           }
        }

      //! Cash-flow convexity
      /*! The convexity of a string of cash flows is defined as
	            \f[
	            C = \frac{1}{P} \frac{\partial^2 P}{\partial y^2}
	            \f]
	            where \f$ P \f$ is the present value of the cash flows according to the given IRR \f$ y \f$.
	        */
      public static double convexity(List<CashFlow> cashflows, InterestRate rate) { return convexity(cashflows, rate, null); }
      public static double convexity(List<CashFlow> cashflows, InterestRate rate, Date settlementDate) 
        {
           if (cashflows.Count == 0)
              return 0.0;

           if (settlementDate == null) settlementDate = Settings.evaluationDate();

           DayCounter dayCounter = rate.dayCounter();

           double P = 0;
           double d2Pdy2 = 0;
           double y = rate.rate();
           int N = (int)rate.frequency();

           
           foreach (CashFlow cashflow in cashflows.Where(cashflow => !cashflow.hasOccurred(settlementDate))) 
           {
              double t = dayCounter.yearFraction(settlementDate, cashflow.date());
              double c = cashflow.amount();
              double B = rate.discountFactor(t);

              P += c * B;

              switch (rate.compounding()) 
              {
                 case Compounding.Simple:
                    d2Pdy2 += c * 2.0 * B * B * B * t * t;
                    break;
                    
                 case Compounding.Compounded:
                    d2Pdy2 += c * B * t * (N * t + 1) / (N * (1 + y / N) * (1 + y / N));
                    break;
                    
                 case Compounding.Continuous:
                    d2Pdy2 += c * B * t * t;
                    break;
                    
                 case Compounding.SimpleThenCompounded:
                    if (t <= 1.0 / N)
                       d2Pdy2 += c * 2.0 * B * B * B * t * t;
                    else
                       d2Pdy2 += c * B * t * (N * t + 1) / (N * (1 + y / N) * (1 + y / N));
                    break;
                    
                 default:
                    throw new ArgumentException("unknown compounding convention (" + rate.compounding() + ")");
              }
           }

           // no cashflows
           
           if (P == 0) return 0;
           return d2Pdy2 / P;
        }

      //! Basis-point value
      /*! Obtained by setting dy = 0.0001 in the 2nd-order Taylor
            series expansion.
        */
      public static double basisPointValue(List<CashFlow> leg, InterestRate y, Date settlementDate) 
      {
         if (leg.Count == 0)
           return 0.0;


         double shift = 0.0001;
         double dirtyPrice = CashFlows.npv(leg, y, settlementDate);
         double modifiedDuration = CashFlows.duration(leg, y, Duration.Type.Modified, settlementDate);
         double convexity = CashFlows.convexity(leg, y, settlementDate);

         double delta = -modifiedDuration*dirtyPrice;

         double gamma = (convexity/100.0)*dirtyPrice;

         delta *= shift;
         gamma *= shift*shift;

         return delta + 0.5*gamma;
      }

      //! Yield value of a basis point
      /*! The yield value of a one basis point change in price is
            the derivative of the yield with respect to the price
            multiplied by 0.01
        */
      public static double yieldValueBasisPoint(List<CashFlow> leg, InterestRate y, Date settlementDate) 
      {
         if (leg.Count == 0)
            return 0.0;

         double shift = 0.01;
         double dirtyPrice = CashFlows.npv(leg, y, settlementDate);
         double modifiedDuration = CashFlows.duration(leg, y, Duration.Type.Modified, settlementDate);
         
         return (1.0/(-dirtyPrice*modifiedDuration))*shift;
      }

    }
}
