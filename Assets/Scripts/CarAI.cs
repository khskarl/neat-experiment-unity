using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SharpNeat.Phenomes;

enum SensorType
{
	Left,
	LeftFront,
	Front,
	RightFront,
	Right
}

public class CarAI : UnitController
{
	public bool isHumanControlled = false;

	public float speed = 100f;
	public float maxSpeed = 10f;
	public float _currSpeed = 0f;
	public float _averageSpeed = 0f;
	public float _averageTurn = 0f;
	public float _averageDisplacement = 0f;
	public float _numHits = 0;

	Vector3 _previousPosition = Vector3.zero;

	// Sensor parameters
	float sensorRange = 2.5f;

	// Neat
	bool _isRunning;
	IBlackBox _box;

	// Inputs
	public float forwardInput = 0.0f;
	public float turnInput = 0.0f;

	// Components
	private Rigidbody _rigidbody = null;

	void Start ()
	{
		_rigidbody = GetComponent<Rigidbody> ();
		_previousPosition = transform.position;
		InvokeRepeating ("DisplacementDataUpdate", 1f, 1f);
	}

	void Update ()
	{
		if (isHumanControlled) {
			forwardInput = Input.GetAxisRaw ("Vertical");
			turnInput = Input.GetAxisRaw ("Horizontal");
		}

		//Debug.DrawRay (transform.position, transform.up * 0.3f);
		Debug.DrawRay (transform.position, transform.up * sensorRange);
		//Debug.DrawRay (transform.position, (transform.up - transform.right).normalized * sensorRange);
		//Debug.DrawRay (transform.position, (transform.up + transform.right).normalized * sensorRange);
		//Debug.DrawRay (transform.position, -transform.right * sensorRange);
		//Debug.DrawRay (transform.position, transform.right * sensorRange);

	}

	void DisplacementDataUpdate ()
	{
		//Debug.Log ("Previous: " + _previousPosition);
		//Debug.Log ("Current : " + transform.position);
		//Debug.Log ("Displacement: " + );
		float displacement = (transform.position - _previousPosition).magnitude;
		_averageDisplacement = (displacement + _averageDisplacement) / 2;
		_previousPosition = transform.position;
	}

	void FixedUpdate ()
	{
		

		if (isHumanControlled == false && _isRunning) {
			NeatInputUpdate ();
		}

		_rigidbody.AddForce (transform.up * speed * forwardInput);
		_rigidbody.AddTorque (-transform.forward * turnInput * 0.4f);
		_currSpeed = _rigidbody.velocity.magnitude;
		_averageSpeed = (_averageSpeed + _currSpeed) / 2f;

		if (_rigidbody.velocity.magnitude > maxSpeed) {
			_rigidbody.velocity = Vector3.ClampMagnitude (_rigidbody.velocity, maxSpeed);
		}		
	}

	void NeatInputUpdate ()
	{
		ISignalArray neatInputs = _box.InputSignalArray;

		RaycastHit hit;
		float dx = 0.1f;
		{
			int numHits = 0;
			float average = 0.0f;
			for (int i = 0; i < 3; i++) {
				if (Physics.Raycast (transform.position, transform.up + transform.right * (dx * (i - 1)), out hit, sensorRange)) {
					average += 1 - hit.distance / sensorRange;
					numHits += 1;
				}	
			}

			numHits = Mathf.Max (numHits, 1);
			average /= numHits;
			neatInputs [0] = average;
		}


		if (Physics.Raycast (transform.position, transform.up - transform.right, out hit, sensorRange)) {
			neatInputs [1] = 1 - hit.distance / sensorRange;
		}

		if (Physics.Raycast (transform.position, transform.up + transform.right, out hit, sensorRange)) {
			neatInputs [2] = 1 - hit.distance / sensorRange;
		}

		if (Physics.Raycast (transform.position, -transform.right, out hit, sensorRange)) {
			neatInputs [3] = 1 - hit.distance / sensorRange;
		}
			
		if (Physics.Raycast (transform.position, transform.right, out hit, sensorRange)) {
			neatInputs [4] = 1 - hit.distance / sensorRange;
		}


		_box.Activate ();

		ISignalArray neatOutputs = _box.OutputSignalArray;

		forwardInput = (float)neatOutputs [0] * 2 - 1;
		turnInput = (float)neatOutputs [1] * 2 - 1;
	}


	public override void Stop ()
	{
		this._isRunning = false;
	}

	public override void Activate (IBlackBox box)
	{
		this._box = box;
		this._isRunning = true;
	}

	public override float GetFitness ()
	{	
		int maxNumHits = 30;
		float hitPenalty = Mathf.Min (_numHits, maxNumHits) / maxNumHits;

		float speedFitness = Mathf.Min (_averageSpeed, maxSpeed) / maxSpeed;
		float displacementFitness = Mathf.Min (_averageDisplacement, maxSpeed) / maxSpeed;
		float fitness = (speedFitness * 0.6f + displacementFitness * 0.4f) - 0.15f * hitPenalty;
		return Mathf.Max (fitness, 0.0f);
	}


	void OnCollisionEnter (Collision collision)
	{
		_numHits++;
	}
}
