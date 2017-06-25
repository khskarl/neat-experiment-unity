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

	// Sensor parameters
	float sensorRange = 3.0f;

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
	}

	void Update ()
	{
		if (isHumanControlled) {
			forwardInput = Input.GetAxisRaw ("Vertical");
			turnInput = Input.GetAxisRaw ("Horizontal");
		}

		//Debug.DrawRay (transform.position, transform.up * 0.3f);
		Debug.DrawRay (transform.position, transform.up * sensorRange);
		Debug.DrawRay (transform.position, (transform.up - transform.right).normalized * sensorRange);
		Debug.DrawRay (transform.position, (transform.up + transform.right).normalized * sensorRange);
	}

	void FixedUpdate ()
	{
		if (isHumanControlled == false && _isRunning) {
			NeatInputUpdate ();
		}

		_rigidbody.AddForce (transform.up * speed * forwardInput);
		_rigidbody.AddTorque (-transform.forward * turnInput * 0.4f);
		_currSpeed = _rigidbody.velocity.magnitude;

		if (_rigidbody.velocity.magnitude > maxSpeed) {
			_rigidbody.velocity = Vector3.ClampMagnitude (_rigidbody.velocity, maxSpeed);
		}		
	}

	void NeatInputUpdate ()
	{
		ISignalArray neatInputs = _box.InputSignalArray;
		neatInputs [0] = 0.0;
		RaycastHit hit;

		if (Physics.Raycast (transform.position, transform.up, out hit, sensorRange)) {
			neatInputs [0] = 1 - hit.distance / sensorRange;
		}

		if (Physics.Raycast (transform.position, transform.up - transform.right, out hit, sensorRange)) {
			neatInputs [1] = 1 - hit.distance / sensorRange;
		}

		if (Physics.Raycast (transform.position, transform.up + transform.right, out hit, sensorRange)) {
			neatInputs [2] = 1 - hit.distance / sensorRange;
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
		return _currSpeed;
	}
}
