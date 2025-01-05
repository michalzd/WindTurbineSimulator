using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTurbine
{
    class TurboGenerator
    {
        public int time;
        public double windVelocity;

        protected Turbina turbina;
        protected Generator generator;

        public TurboGenerator ()
        {
            turbina = new Turbina();
            generator = new Generator();
        }


        public void GetLabels(string[] labels)
        {
            labels[1] = "Sec"; 
            labels[2] = "Wind";
            labels[3] = "Rpm";
            labels[4] = "Omega";
            labels[5] = "Lambda";
            labels[6] = "Cp";
            labels[7] = "Power";
            labels[8] = "Torque";
            labels[9] = "LoadTorque";
            labels[10] = "GenPower";


        }

        public double GetMaxCP()
        {
            return turbina.maxcp;
        }
        

        public void Simulate(int currenttime, string[] wyniki)
        {
            this.time = currenttime;
            double sec = currenttime / 10; // rozdzielczość symulacji 0,1 sekund

            // obciążenie turbiny generatorem
            turbina.LoadTorque = generator.Torque;
            turbina.Simulate(currenttime, windVelocity);

            generator.Rpm = turbina.Rpm;
            generator.Omega = turbina.Omega;
            generator.PowerIn = turbina.Power;
            generator.TorqueIn = turbina.Torque;
            generator.Simulate(time);

            wyniki[1] = sec.ToString();
            wyniki[2] = this.windVelocity.ToString();
            wyniki[3] = turbina.Rpm.ToString();
            wyniki[4] = turbina.Omega.ToString();
            wyniki[5] = turbina.Lamda.ToString();
            wyniki[6] = turbina.Cp.ToString();
            wyniki[7] = turbina.Power.ToString();
            wyniki[8] = turbina.Torque.ToString();
            wyniki[9] = turbina.LoadTorque.ToString();
            wyniki[10] = generator.Power.ToString(); 

        }

    }

}
