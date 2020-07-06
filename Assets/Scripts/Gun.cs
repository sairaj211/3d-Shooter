using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour {

	public enum FireMode{Auto, Burst, Single};
	public FireMode fireMode;
	public int burstCount;

	public Transform[] projectileSpawn;
	public Transform muzzle;
	public Projectile projectile;
	public float msBetweenShots = 100;
	public float muzzleVelocity = 35; 
	public int projectilesPerMag = 10;
	public float reloadTime = .3f;

	[Header("Effects")]
	public Transform shell;
	public Transform shellEjection;
	MuzzleFlash muzzleflash;
	float nextShotTime;

	
	[Header("Recoil")]
	public Vector2 kickMinMax = new Vector2(0.05f, 0.2f);
	public float recoilMoveSettleTime = .1f;
	Vector3 recoilSmoothDampVelocity;

	[Header("Audio")]
	public AudioClip shootAudio;
	public AudioClip reloadAudio;

	bool triggerReleasedSinceLastShot;
	int shotsRemainingInBurst;

	//reloading
	int projectilesRemainingInMag;
	bool isReloading;

	//float recoilRotSmoothDampVelocity;
	//float recoilAngle;

	void Start(){
		muzzleflash = GetComponent<MuzzleFlash> ();
		shotsRemainingInBurst = burstCount;
		projectilesRemainingInMag = projectilesPerMag;
	}

	void Update(){
		//animate recoil
		transform.localPosition = Vector3.SmoothDamp( transform.localPosition, Vector3.zero, ref recoilSmoothDampVelocity, recoilMoveSettleTime);
		//recoilAngle = Mathf.SmoothDamp (recoilAngle, 0, ref recoilRotSmoothDampVelocity, .1f);
		//transform.localEulerAngles = transform.localEulerAngles + Vector3.left * recoilAngle;

		if (!isReloading && projectilesRemainingInMag == 0) {
			Reload ();
		}
	}

	public void Shoot() {

		if (!isReloading && Time.time > nextShotTime && projectilesRemainingInMag >0) {

			if (fireMode == FireMode.Burst) {
				if (shotsRemainingInBurst == 0) {
					return;
				}
				shotsRemainingInBurst--;
			}

			if (fireMode == FireMode.Single) {
				if (!triggerReleasedSinceLastShot) {
					return;
				}			
			}

			for (int i = 0; i < projectileSpawn.Length; ++i) {
				if (projectilesRemainingInMag == 0) {
					break;
				}
				projectilesRemainingInMag--;
				nextShotTime = Time.time + msBetweenShots / 1000;
				Projectile newProjectile = Instantiate (projectile, projectileSpawn[i].position, projectileSpawn[i].rotation) as Projectile;
				newProjectile.SetSpeed (muzzleVelocity);
			}

			Instantiate (shell, shellEjection.position, shellEjection.rotation);
			muzzleflash.Activate ();
			transform.localPosition -= Vector3.forward * Random.Range (kickMinMax.x, kickMinMax.y);
			//recoilAngle += 5f;
			//recoilAngle = Mathf.Clamp (recoilAngle, 0, 30);

			AudioManager.instance.PlaySound (shootAudio, transform.position);
		}
	}
		
	public void Reload(){
		if (!isReloading && projectilesRemainingInMag != projectilesPerMag) {
			StartCoroutine (AnimateReload ());
			AudioManager.instance.PlaySound (reloadAudio, transform.position);
		}
	}

	IEnumerator AnimateReload(){
		isReloading = true;
		yield return new WaitForSeconds (0.2f);

		float reloadSpeed = 1f / reloadTime;
		float percent = 0;
		Vector3 initialRot = transform.localEulerAngles;
		float maxReloadAngle = 30;

		while (percent < 1) {
			percent += Time.deltaTime * reloadSpeed;
			float interpolation = (-Mathf.Pow (percent, 2) + percent) * 4;
			float reloadAngle = Mathf.Lerp (0, maxReloadAngle, interpolation);
			transform.localEulerAngles = initialRot + Vector3.left * reloadAngle;

			yield return null;
		}

		isReloading = false;
		projectilesRemainingInMag = projectilesPerMag;
	}

	public void OnTriggerHold(){
		Shoot ();
		triggerReleasedSinceLastShot = false;
	}

	public void OnTriggerRelease(){
		triggerReleasedSinceLastShot = true;
		shotsRemainingInBurst = burstCount;
	}
}