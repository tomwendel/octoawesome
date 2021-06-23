using engenious;
using OctoAwesome.EntityComponents;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Components
{
    /// <summary>
    /// Baseclass of all SimulationComponents who extend the <see cref="Simulation"/>
    /// </summary>
    public abstract class SimulationComponent : Component
    {

        /// <summary>
        /// Updatemethod of this Component
        /// </summary>
        /// <param name="gameTime">The current gametime</param>
        public abstract void Update(GameTime gameTime);

    }

    /// <summary>
    /// Basisklasse für Simulationskomponenten
    /// </summary>
    public abstract class SimulationComponent<T> : SimulationComponent, IHoldComponent<T>
    {
        /// <summary>
        /// Entities die durch diese Simulationkomponete simuliert werden
        /// </summary>
        protected readonly List<T> values = new();

        /// <summary>
        /// Konstruktor
        /// </summary>
        public SimulationComponent()
        {

        }

        /// <summary>
        /// Fügt eine neue Entity der Simulationskomponente hinzu
        /// </summary>
        /// <param name="value">Neue Entity</param>
        public void Add(T value)
        {
            if (Match(value))
            {
                OnAdd(value);
                values.Add(value);
            }
        }

        /// <summary>
        /// Führt ein Vergleich durch, ob diese Entity in die Komponente eingefügt werden kann
        /// </summary>
        /// <param name="value">Vergleichsentity</param>
        /// <returns>Ergebnis des Vergleiches</returns>
        protected virtual bool Match(T value)
        {
            return true;
        }

        /// <summary>
        /// Internes Event, für das hinzufügen einer Entity
        /// </summary>
        /// <param name="value">Neue Entity</param>
        /// <returns>Ergebnis</returns>
        protected abstract void OnAdd(T value);

        /// <summary>
        /// Entfernt eine Entity aus der Simulationskomponente
        /// </summary>
        /// <param name="value"></param>
        public void Remove(T value)
        {
            if (values.Contains(value))
            {
                OnRemove(value);
                values.Remove(value);
            }
        }

        /// <summary>
        /// Internes Event, für das entfernen einer Entity
        /// </summary>
        /// <param name="value">Neue Entity</param>
        /// <returns>Ergebnis</returns>
        protected abstract void OnRemove(T value);
    }

    /// <summary>
    /// Basisklasse für Simulationskomponenten
    /// </summary>
    public abstract class SimulationComponent<T, S, C1> : SimulationComponent, IHoldComponent<T>
        where T : IContainsComponents
        where S : SimulationComponentRecord<T, C1>
        where C1 : Component
    {

        protected readonly List<S> values = new();

        /// <summary>
        /// Adds a new value of <typeparamref name="T"/> to this Component
        /// </summary>
        /// <param name="value">an instance of <typeparamref name="T"/> to add</param>
        public void Add(T value)
        {
            if (Match(value))
            {
                values.Add(OnAdd(value));
            }
        }



        /// <summary>
        /// Is called during <see cref="Add(T)"/> to convert <paramref name="value"/> from <typeparamref name="T"/> to <typeparamref name="S"/>
        /// </summary>
        /// <param name="value">instance of <typeparamref name="T"/> that is passed to <see cref="Add(T)"/></param>
        /// <returns>Converted <paramref name="value"/> as <typeparamref name="S"/></returns>
        protected virtual S OnAdd(T value)
            => (S)new SimulationComponentRecord<T, C1>(value, value.GetComponent<C1>());

        /// <summary>
        /// Removes an instance of <typeparamref name="T"/> that is previouse added with <see cref="Add(T)"/>
        /// </summary>
        /// <param name="value">an instance of <typeparamref name="T"/> to Remove</param>
        public void Remove(T value)
        {
            OnRemove(value);
            values.RemoveAll(c => Compare(c, value));
        }

        /// <summary>
        /// Is called during <see cref="Remove(T)"/> to convert <paramref name="value"/> from <typeparamref name="T"/> to <typeparamref name="S"/>
        /// </summary>
        /// <param name="value">instance of <typeparamref name="T"/> that is passed to <see cref="Add(T)"/></param>
        /// <returns>Converted <paramref name="value"/> as <typeparamref name="S"/></returns>
        protected virtual void OnRemove(T value) { }

        protected virtual bool Compare(S left, T right)
            => Equals(left.Value, right);

        /// <summary>
        /// Führt ein Vergleich durch, ob diese Entity in die Komponente eingefügt werden kann
        /// </summary>
        /// <param name="value">Vergleichsentity</param>
        /// <returns>Ergebnis des Vergleiches</returns>
        protected virtual bool Match(T value)
            => value.ContainsComponent<C1>();

        /// <summary>
        /// Updatemethode der Entity
        /// </summary>
        /// <param name="gameTime">Spielzeit</param>
        public override void Update(GameTime gameTime)
        {
            foreach (var value in values)
                UpdateValue(gameTime, value);
        }

        /// <summary>
        /// Internes Event, für das Updaten der Simulationskomponente
        /// </summary>
        /// <param name="gameTime">Spielzeit</param>
        /// <param name="value">Entity die geupdatet werden muss</param>
        protected abstract void UpdateValue(GameTime gameTime, S value);

    }

    /// <summary>
    /// Basisklasse für Simulationskomponenten
    /// </summary>
    public abstract class SimulationComponent<T, S, C1, C2> : SimulationComponent, IHoldComponent<T>
        where T : IContainsComponents
        where S : SimulationComponentRecord<T, C1, C2>
        where C1 : Component
        where C2 : Component
    {
        protected readonly List<S> values = new();

        /// <summary>
        /// Adds a new value of <typeparamref name="T"/> to this Component
        /// </summary>
        /// <param name="value">an instance of <typeparamref name="T"/> to add</param>
        public void Add(T value)
        {
            if (Match(value))
            {
                values.Add(OnAdd(value));
            }
        }

        /// <summary>
        /// Is called during <see cref="Add(T)"/> to convert <paramref name="value"/> from <typeparamref name="T"/> to <typeparamref name="S"/>
        /// </summary>
        /// <param name="value">instance of <typeparamref name="T"/> that is passed to <see cref="Add(T)"/></param>
        /// <returns>Converted <paramref name="value"/> as <typeparamref name="S"/></returns>
        protected virtual S OnAdd(T value)
            => (S)new SimulationComponentRecord<T, C1, C2>(value, value.GetComponent<C1>(), value.GetComponent<C2>());

        /// <summary>
        /// Removes an instance of <typeparamref name="T"/> that is previouse added with <see cref="Add(T)"/>
        /// </summary>
        /// <param name="value">an instance of <typeparamref name="T"/> to Remove</param>
        public void Remove(T value)
        {
            OnRemove(value);
            values.RemoveAll(c => Compare(c, value));
        }

        /// <summary>
        /// Is called during <see cref="Remove(T)"/> to convert <paramref name="value"/> from <typeparamref name="T"/> to <typeparamref name="S"/>
        /// </summary>
        /// <param name="value">instance of <typeparamref name="T"/> that is passed to <see cref="Add(T)"/></param>
        /// <returns>Converted <paramref name="value"/> as <typeparamref name="S"/></returns>
        protected virtual void OnRemove(T value) { }

        protected virtual bool Compare(S left, T right)
            => Equals(left.Value, right);

        /// <summary>
        /// Führt ein Vergleich durch, ob diese Entity in die Komponente eingefügt werden kann
        /// </summary>
        /// <param name="value">Vergleichsentity</param>
        /// <returns>Ergebnis des Vergleiches</returns>
        protected virtual bool Match(T value)
            => value.ContainsComponent<C1>()
                && value.ContainsComponent<C2>();

        /// <summary>
        /// Updatemethode der Entity
        /// </summary>
        /// <param name="gameTime">Spielzeit</param>
        public override void Update(GameTime gameTime)
        {
            //TODO: Ändern
            foreach (var value in values)
                UpdateValue(gameTime, value);
        }

        /// <summary>
        /// Internes Event, für das Updaten der Simulationskomponente
        /// </summary>
        /// <param name="gameTime">Spielzeit</param>
        /// <param name="value">Entity die geupdatet werden muss</param>
        protected abstract void UpdateValue(GameTime gameTime, S value);

    }

    /// <summary>
    /// Basisklasse für Simulationskomponenten
    /// </summary>
    public abstract class SimulationComponent<T, S, C1, C2, C3> : SimulationComponent, IHoldComponent<T>
        where T : IContainsComponents
        where S : SimulationComponentRecord<T, C1, C2, C3>
        where C1 : Component
        where C2 : Component
        where C3 : Component
    {

        protected readonly List<S> values = new();

        /// <summary>
        /// Adds a new value of <typeparamref name="T"/> to this Component
        /// </summary>
        /// <param name="value">an instance of <typeparamref name="T"/> to add</param>
        public void Add(T value)
        {
            if (Match(value))
            {
                values.Add(OnAdd(value));
            }
        }


        /// <summary>
        /// Is called during <see cref="Add(T)"/> to convert <paramref name="value"/> from <typeparamref name="T"/> to <typeparamref name="S"/>
        /// </summary>
        /// <param name="value">instance of <typeparamref name="T"/> that is passed to <see cref="Add(T)"/></param>
        /// <returns>Converted <paramref name="value"/> as <typeparamref name="S"/></returns>
        protected virtual S OnAdd(T value)
            => (S)new SimulationComponentRecord<T, C1, C2, C3>(value, value.GetComponent<C1>(), value.GetComponent<C2>(), value.GetComponent<C3>());

        /// <summary>
        /// Removes an instance of <typeparamref name="T"/> that is previouse added with <see cref="Add(T)"/>
        /// </summary>
        /// <param name="value">an instance of <typeparamref name="T"/> to Remove</param>
        public void Remove(T value)
        {
            OnRemove(value);
            values.RemoveAll(c => Compare(c, value));
        }

        /// <summary>
        /// Is called during <see cref="Remove(T)"/> to convert <paramref name="value"/> from <typeparamref name="T"/> to <typeparamref name="S"/>
        /// </summary>
        /// <param name="value">instance of <typeparamref name="T"/> that is passed to <see cref="Add(T)"/></param>
        /// <returns>Converted <paramref name="value"/> as <typeparamref name="S"/></returns>
        protected virtual void OnRemove(T value) { }


        protected virtual bool Compare(S left, T right)
            => Equals(left.Value, right);

        /// <summary>
        /// Führt ein Vergleich durch, ob diese Entity in die Komponente eingefügt werden kann
        /// </summary>
        /// <param name="value">Vergleichsentity</param>
        /// <returns>Ergebnis des Vergleiches</returns>
        protected virtual bool Match(T value)
            => value.ContainsComponent<C1>()
                && value.ContainsComponent<C2>()
                && value.ContainsComponent<C3>();

        /// <summary>
        /// Updatemethode der Entity
        /// </summary>
        /// <param name="gameTime">Spielzeit</param>
        public override void Update(GameTime gameTime)
        {
            //TODO: Ändern
            foreach (var value in values)
                UpdateValue(gameTime, value);
        }

        /// <summary>
        /// Internes Event, für das Updaten der Simulationskomponente
        /// </summary>
        /// <param name="gameTime">Spielzeit</param>
        /// <param name="value">Entity die geupdatet werden muss</param>
        protected abstract void UpdateValue(GameTime gameTime, S value);

    }

    public record SimulationComponentRecord<T, C1>(T Value, C1 Component);
    public record SimulationComponentRecord<T, C1, C2>(T Value, C1 Component1, C2 Component2);
    public record SimulationComponentRecord<T, C1, C2, C3>(T Value, C1 Component1, C2 Component2, C3 Component3);
}

