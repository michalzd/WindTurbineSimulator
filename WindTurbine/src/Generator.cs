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

        static double MaxPwm = 255;
        public int    LoadPwm;


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

        // wspolczynniki wielomianu PSF
        public double Umin3;
        public double Un3; 
        public double a;
        public double b;



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
            this.LoadPwm = 0;

            /* punkty charakterystyki PSF */
            psf = new PSF();
            psf.A = new Punkt( 25, 0.1);    // start od 25 V
            psf.B = new Punkt(200, 3.0);    // obroty i moc nominalne
            psf.C = new Punkt(210, 2.5);    // początek hamowania

            ObliczWspolczynnikiPSF();

        }

        /* wspolczynniki wielomianu PSF  I => f(U) = a*U + b*U^3 */
        public void ObliczWspolczynnikiPSF()
        {
            this.Umin3 = psf.A.U * psf.A.U * psf.A.U;
            this.Un3 = psf.B.U * psf.B.U * psf.B.U;
            double ku = psf.B.U / psf.A.U; 

            // b = (ku * Imin - In) / (ku * Umin3 - Un3)
            // a = (Imin - Um3 * b) / Umin
            double bb = (ku * this.Umin3 - this.Un3);
            this.b = (ku * psf.A.I - psf.B.I) / bb;
            this.a = (psf.A.I - this.Umin3 * this.b) / psf.A.U;
            
            Console.WriteLine(" Wspolczynniki a=" + this.a.ToString() + "   b=" + this.b.ToString());
        }


        // // opory własne generatora zależą od obrotów
        // let selfLoss = 0.0002 * msg.payload.wt.Rpm;

        public void Simulate(int time)
        {
            this.lastI = this.I;
            this.lastU = this.U;

            // prąd oraz napięcie wyjściowe 
            this.SEM = DeltaV * this.Rpm;
            this.I = this.SEM / (this.CurrentLoad + RezystancjaWewn);
            this.U = this.SEM - (this.I * RezystancjaWewn);

            // this.CurrentLoad = LoadConstant();
            // this.CurrentLoad = LoadFindK();
            
            this.CurrentLoad = LoadPSF();

            // obliczam nowy prąd, a wg niego moc
            this.I = this.U / this.CurrentLoad;

            // moc elektryczna
            this.ElePower = this.U * this.I;

            SetPWM(); // tutaj do sterownika PWM

            // całkowita moc oraz całkowity moment obrotowy ze stratami mechanicznymi i elektr.
            this.Power = this.SEM * this.I;
            this.Torque = this.Power / this.Omega;  // += selfLoss;
            
        }


        /* sterownik PWM
         * ustala sygnał PWM tak aby uzyskać wymagany prąd obciążenia
         */
        public void SetPWM()
        {

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

            double currOmega;

            /* będą dwie odmiany tego algorytmu.
             * opisany na podanej stronie dotyczy możliwości obserwacji obrotów wału, 
             * bazuje więc na faktycznej wartości prędkości kątowej 
             * lecz w rzeczywistym układzie nie ma obserwatora prędkości generatora,
             * więc jedynie można to odtworzyć bazując na charakterystyce U=f(n) 
             */
            currOmega = this.Omega;     // przypadek 1. pomiar obrotów istnieje
            currOmega = this.U;         // przypadek 2. brak pomiar obrotów 

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


        /* symulacja obciążenia 
         * algorytm wg krzywej PSF 
         */
        protected double LoadPSF()
        {
            double Load = this.CurrentLoad;
            
            /* jest to algorytm obciążenia wg krzywej okreslonej wielomianem trzeciego stopnia
             * I => f(U) = a*U + b*U^3 
             * bazuje więc na faktycznej wartości napięcia 
             */
            
            // włączymy obciązenie po uzyskaniu odpowieniego napięcia punktu A
            // z niewielką histerezą 5V (napięcie spada po obciążeniu)
            double minimumU = psf.A.U + 5;
           
            if (this.U < minimumU)
            {
                Load = InfinityLoad; 
            }
            else if (this.U <= psf.B.U) 
            {
                double parta = this.a * this.U;
                double partb = this.b * this.U * this.U * this.U;
                double calculatedI =  parta + partb;
                Load = this.U / calculatedI;
            }
            else
            {
                // początek hamowania
                double parta = this.a * this.U;
                double partb = this.b * this.U * this.U * this.U;
                double calculatedI = parta + partb;
                Load = this.U / calculatedI;
            }

            return Load;
        }





        /* symulacja obciążenia 
         * prosty algorytm mocy max
         */
        protected double LoadMaxPw()
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
                                    
                

                // przeliczam na obciązenie: R = U^2/ P
                //Load = (this.U * this.U) / pg;

            }

            return Load;
        }

    }


}
