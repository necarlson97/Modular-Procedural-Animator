using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackBuffer {
    // Buffer attacks, so a player's
    // input is still captured while their
    // being is busy performing the action
    // TODO I think this will need to be expanded
    // to a general input buffer, perhaps
    // made a part of CustomInput - but leaving for now

    // How old an input can be until it is ignored (seconds)
    private const float maxTime = 0.5f;
    // How many inputs can be in queue until new ones are ignored
    private const int maxInputs = 5;

    // Hold the buffered attacks alongside their time
    public Queue<AttackTime> _queue = new Queue<AttackTime>();
    public class AttackTime {
        public Attack attack { get; }
        public float time { get; }
        public AttackTime(Attack attack, float time) {
            this.attack = attack;
            this.time = time;
        }
    }

    public void Add(Attack attack) {
        // Enqueue attack, (so long as the queue is not full)
        // attaching the time it was added,
        // - so it can later be discarded if it becomes old
        if (_queue.Count < maxInputs) {
            _queue.Enqueue(new AttackTime(attack, Time.time));
        }
    }

    public Attack Pop() {
        // Get a ready, (non-stale) attack from the queue,
        // returning null if it is empty
        RemoveStale();
        if (_queue.Count == 0) return null;
        return _queue.Dequeue().attack;
    }

    public Attack Peek() {
        // Return the next attack,
        // but do not remove from queue
        RemoveStale();
        if (_queue.Count == 0) return null;
        return _queue.Peek().attack;
    }

    private void RemoveStale() {
        // Remove stale inputs
        while (_queue.Count > 0 && Time.time - _queue.Peek().time > maxTime) {
            _queue.Dequeue();
        }
    }

    public void Clear() {
        // Clear the queue
        _queue.Clear();
    }
}