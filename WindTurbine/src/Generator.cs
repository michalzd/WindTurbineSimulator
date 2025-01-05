using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTurbine
{

    /* punkty charakterystyki PSF */
    struct Punkt
    {
        public double U;
        public double I;

        public Punkt( double Napiecie, double Prad)
        {
            this.U = Napiecie;
            this.I = Prad;
        }
    }

    class PSF
    {
        public Punkt A;     // start
        public Punkt B;     // obroty i moc nominalne
        public Punkt C;     // początek hamowania
        public Punkt D;     // pełne hamowanie
    }



    class Generator
    {
        // parametry generatora
        static double NominalnaMoc = 750;
        static double NominalneNapiecie = 200;
        static double RezystancjaWewn = 2;
        static double NominalneObroty = 400;


        // zależnośc U/R  DeltaV = 0,5 
        static double DeltaV = NominalneNapiecie / NominalneObroty;

        // bieżące dane:  moc i moment napędowy, obroty
        public double PowerIn;
        public double TorqueIn;
        public double Rpm;
        public double Omega;
        

        // bieżące obciazenie w omach
        static double InfinityLoad = 999999999;
        public double CurrentLoad;


        public double SEM;
        public double U;
        public double I;
        public double Power;
        public double Torque;   // bieżący moment hamujący 
        public double ElePower;

        public double lastU;
        public double lastI;

        // punkty pracy
        PSF psf;


        public Generator()
        {
            this.PowerIn = 0;
            this.TorqueIn = 0;
            this.Rpm = 0;
            this.Omega = 0;
            this.Torque = 0;
            this.Power = 0;
            this.ElePower = 0;
            this.U = 0;
            this.I = 0;
            this.CurrentLoad = InfinityLoad;

            /* punkty charakterystyki PSF */
            psf = new PSF();
            psf.A = new Punkt( 25, 0.1);    // start od 25 V
            psf.B = new Punkt(200, 3.0);    // obroty i moc nominalne
            psf.C = new Punkt(210, 2.5);    // początek hamowania

        }


        // // opory własne generatora zależą od obrotów
        // let selfLoss = 0.0002 * msg.payload.wt.Rpm;

        public void Simulate(int time)
        {
            this.lastI = this.I;
            this.lastU = this.U;

            // prąd oraz napięcie wyjściowe 
            this.SEM = DeltaV * this.Rpm;
            this.I = this.SEM / this.CurrentLoad;
            this.U = this.SEM - (this.I * RezystancjaWewn);

            // this.CurrentLoad = LoadConstant();
            this.CurrentLoad = LoadFindK();

            // obliczam nowy prąd, a wg niego moc
            this.I = this.U / this.CurrentLoad;

            // moc elektryczna
            this.ElePower = this.U * this.I;

            // całkowita moc oraz całkowity moment obrotowy ze stratami mechanicznymi i elektr.
            this.Power = this.SEM * this.I;
            this.Torque = this.Power / this.Omega;  // += selfLoss;
            
        }

        /* symulacja obciążenia stałym rezystorem
         * do testu sto omów
         */
        protected double LoadConstant()
        {
            // włączymy obciązenie po uzyskaniu odpowienich obr. wg napięcia
            // a wyłączymy z histerezą 5V 
            double Histereza = 5;

            double Resistor = this.CurrentLoad;

            if (this.U > psf.A.U) Resistor = 100;
            else if( (this.U - Histereza) < psf.A.U ) Resistor = 99999999;

            return Resistor;
             
        }


        /* symulacja obciążenia 
         * algorytm wg https://elektrownie-wiatrowe.opx.pl/Menu/Model_generator.html
         */
        public double k = 1;
        public double deltak = 0; 

        protected double LoadFindK()
        {
            double Load = this.CurrentLoad;

            // włączymy obciązenie po uzyskaniu odpowienich obr. wg napięcia
            // a wyłączymy z histerezą 5V 
            double Histereza = 5;
            if (Load < InfinityLoad)
            {
                if ((this.U - Histereza) < psf.A.U) Load = InfinityLoad;
                this.k = 1;
                this.deltak = 1;
            }
            else if (this.U > psf.A.U)
            {
                double currpg = this.U * this.I;
                double deltap = currpg - this.ElePower;
                // zestaw równań (8)
                double kzad = this.k;
                if (this.deltak >= 0 && deltap > 0) kzad++;
                if (this.deltak >= 0 && deltap < 0) kzad--;
                if (this.deltak < 0 && deltap > 0) kzad--;
                if (this.deltak < 0 && deltap < 0) kzad++;

                // możliwa moc generatora wg Pg = k * Omega do trzeciej
                // wsp wzmocnienia chcę mieć w setnych częściach
                double pg = kzad / 100;
                pg = pg * this.Omega * this.Omega * this.Omega;

                // przeliczam na obciązenie: R = U^2/ P
                Load = (this.U * this.U) / pg;

                this.deltak = kzad - this.k;
                this.k = kzad;
            }

            return Load;
        }

    }


}
