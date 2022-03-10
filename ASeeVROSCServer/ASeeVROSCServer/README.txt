Set Up your Avatar parameters in the Config JSON file.

Address Parameters should match the parameters on your avatar prefixed by "/avatar/parameters/", example config should work for 
	VRCFT configured avatars.

The Average steps config value determines how many frame iterations are kept in the average.  This affects smoothing, a higher
	value will make the tracking less susceptible to bad data or noise but slower to track the eye between 2 and 20 seems to be
	a good range

The moving average buffer size config value determines how many good frames are required to pass tracking data as valid.  As an
	example: It defaults to 10 this means that if there is a bad frame, for the next 10 frames it will be treated as not tracking
	the eye.  The purpose of this value to filter out situations where the eye tracker loses tracking and starts creating false 
	tracks, as these false tracks tend to not be consistent tracking blocks.  Good values are between 0 and 50.


There are two methods for configuring normalization:
	One normalizes the values for eye center based on the range the eye tracker can output (0-1) and uses multipliers to account
		for the eye tracking not using the full range.  If you are using this normalization schema make sure to set the maximum
		and minimum values to 1 and 0 respectively.

	The other schema normalizes the value around the the actual practical values output (something like 0.2-0.8).  This can be
		more accurate but the range must be centered around looking straight ahead (if center is .5 range should be +- some 
		amount on each end).  
		IF YOU USE THIS SCHEMA THE MULTIPLIER VALUES MUST BE SET TO 1.

The next plans for this application are to implement a UI and wizard that allows for configuration in a more user friendly manner.


Note: for those who are more programming savvy, included with the project is an implementation of the 1Euro eye tracking smoothing
	algorithm.  I haven't done testing to detemine if it is more effective than a simple moving average as I have implemented. If
	you decide to test it out and find it preferable please let me know!

