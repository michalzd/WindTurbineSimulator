using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTurbine
{
       
    class PID
    {
        public double ki;
        public double kp;
        public double kd;

        public double outputSum;
        public double lastInput;

        public PID()
        {
            lastInput = 0;
            outputSum = 0;
        }

        // człon PID, obliczenie wszystkich błędow
        public double Compute(double requiresValue, double currentValue)
        {
            /*Compute all the working error variables*/
            double error = requiresValue - currentValue;
            double dInput = currentValue - lastInput;
            outputSum += (ki * error);
            lastInput = currentValue;
            outputSum -= kp * dInput;

            double output = 0;
            output = kp * error + outputSum - kd * dInput;

            return output;
        }

    }



    class Turbina
    {
        static double DlugoscLopaty = 1;    // metr
        static double MasaLopaty = 1;       // kilogram
        static double Pole = 3.141592 * DlugoscLopaty * DlugoscLopaty;

        // gestosc powietrza, przyjme stałą
        static double ro = 1.168;

        // prędkość obrotowa kątowa
        public double Omega;
        public double Rpm;
        public double DeltaOmega;

        // bieżąca moc i moment obrotowy
        public double Power;
        public double Torque;
        public double LoadTorque;
        public double JTorque;


        // współczynniki turbiny 
        public double Lamda;
        public double Cp;
        public double maxcp;


        // PID
        PID pid;

        public Turbina()
        {

            pid = new PID
            {
                kp = 0,
                ki = 0.20, 
                kd = 0.15
            };
             

            this.Rpm = 1;
            this.Omega = RpmToOmega(this.Rpm);
            this.DeltaOmega = 0;
            this.Lamda = 1;
            this.LoadTorque = 0;
            this.Power = 0;
            this.Torque = 0;
            this.maxcp = 0;
        }


        static public double RpmToOmega(double rpm)
        {
            return (rpm * 6.283185) / 60;
        }


        // funkcja cp=f(lambda)
        public double functionCp(double lambda)
        {
            // współczynniki wielomianu w funkcji cp=f(lambda)
            double min = 0.0111;
            double a = 0.02021;
            double b = -0.0712;
            double c = 0.1026;
            double d = -0.01674;
            double e = 0.000476;
            /*
            double min = 0.001336;
            double a = 0.083368;
            double b = -0.1838;
            double c = 0.118605;
            double d = -0.01773;
            double e = 0.000756;*/

            double fcp = a
                      + b * lambda
                      + c * lambda * lambda 
                      + d * lambda * lambda * lambda 
                      + e * lambda * lambda * lambda * lambda;
            if (fcp < min) fcp = min;  // minimalne cp
            if (maxcp < fcp) maxcp = fcp;

            return fcp;
        }

        
        // całkowite obciążenie turbiny 
        public double getLoad()
        {
            // opory własne turbiny zależą od obrotów, przyjąłem narastające liniowo
            double selfLoss = this.Rpm * 0.0005;
            return selfLoss + this.LoadTorque;
        }
        


        public void Simulate(int time, double currentWind)
        {
            // obliczenie aktualnego współczynnika cp
            if(currentWind==0)
            {
                this.Lamda = 0;
                this.Cp = 0;
            }
            else
            {
                this.Lamda = this.Omega * DlugoscLopaty / currentWind;
                this.Cp = functionCp(this.Lamda);
            }
            

            // jaka jest moc aktualna
            this.Power = this.Cp * ro * Pole * currentWind * currentWind * currentWind / 2;

            // moment bezwładnosci trzech łopat, zakładając ten sam przekrój łopaty 
            // mogę użyć twierdzenia Steinera dla koła: 1/2 mR^2
            double selfJ = 3 * MasaLopaty * DlugoscLopaty * DlugoscLopaty / 2;
            this.JTorque = selfJ * this.DeltaOmega;

            // moment obrotowy, pochodzi od mocy wiatru i bezwładności
            if (this.Omega == 0) this.Torque = 0;
            else this.Torque = this.Power / this.Omega - this.JTorque;

            // aktualny momenmt obciązenia 
            this.LoadTorque = getLoad();

            // człon PID, obliczenie wszystkich błędow
            double nextRpm = pid.Compute(this.Torque, this.LoadTorque);
            double nextOmega = RpmToOmega(nextRpm);

            this.DeltaOmega = nextOmega - this.Omega;
            this.Rpm = nextRpm;
            this.Omega = nextOmega;

        }


    }

}
