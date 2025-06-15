'use client';

import { useState, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faExclamationTriangle,
  faTimes,
  faEye,
  faEyeSlash,
  faCheck,
  faStar
} from '@fortawesome/free-solid-svg-icons';
import { teamsApi } from '@/utils/api';
import { TeamTier } from '@/types/team';

interface TeamCreationFormData {
  teamName: string;
  subdomain: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerEmail: string;
  ownerPassword: string;
  confirmPassword: string;
  tier: TeamTier;
  primaryColor: string;
  secondaryColor: string;
}

interface TierInfo {
  name: string;
  price: string;
  description: string;
  features: string[];
  limits: string;
  popular?: boolean;
}

const tierInfo: Record<TeamTier, TierInfo> = {
  [TeamTier.Free]: {
    name: 'Free',
    price: '$0',
    description: 'Perfect for small teams getting started',
    features: [
      'Basic team management',
      'Athlete profiles',
      'Simple event tracking',
      'Email notifications'
    ],
    limits: 'Up to 7 athletes, 2 admins, 2 coaches'
  },
  [TeamTier.Standard]: {
    name: 'Standard',
    price: '$29/month',
    description: 'Great for growing teams with more features',
    features: [
      'Everything in Free',
      'Advanced event management',
      'Custom team branding',
      'Registration windows',
      'SMS notifications',
      'Performance tracking'
    ],
    limits: 'Up to 30 athletes, 5 admins, 5 coaches',
    popular: true
  },
  [TeamTier.Premium]: {
    name: 'Premium',
    price: '$79/month',
    description: 'Full-featured solution for large teams',
    features: [
      'Everything in Standard',
      'Unlimited users',
      'Advanced analytics',
      'Custom integrations',
      'Priority support',
      'Advanced reporting'
    ],
    limits: 'Unlimited athletes, admins, and coaches'
  }
};

export default function SiteRegisterPage() {
  const [formData, setFormData] = useState<TeamCreationFormData>({
    teamName: '',
    subdomain: '',
    ownerFirstName: '',
    ownerLastName: '',
    ownerEmail: '',
    ownerPassword: '',
    confirmPassword: '',
    tier: TeamTier.Standard,
    primaryColor: '#10B981',
    secondaryColor: '#F0FDF4'
  });

  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [error, setError] = useState('');
  const [subdomainError, setSubdomainError] = useState('');
  const [emailError, setEmailError] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isCheckingSubdomain, setIsCheckingSubdomain] = useState(false);
  const [isCheckingEmail, setIsCheckingEmail] = useState(false);

  const searchParams = useSearchParams();

  // Set initial tier based on URL parameter
  useEffect(() => {
    const tierParam = searchParams.get('tier');
    let initialTier = TeamTier.Standard; // Default
    
    if (tierParam) {
      switch (tierParam.toLowerCase()) {
        case 'free':
          initialTier = TeamTier.Free;
          break;
        case 'standard':
          initialTier = TeamTier.Standard;
          break;
        case 'premium':
          initialTier = TeamTier.Premium;
          break;
        default:
          initialTier = TeamTier.Standard;
      }
    }

    setFormData(prev => ({ ...prev, tier: initialTier }));
  }, [searchParams]);

  const updateFormField = (field: keyof TeamCreationFormData, value: string | TeamTier) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // Clear errors when user starts typing
    if (field === 'subdomain') setSubdomainError('');
    if (field === 'ownerEmail') setEmailError('');
    if (error) setError('');
  };

  const checkSubdomainAvailability = async (subdomain: string) => {
    if (!subdomain || subdomain.length < 3) {
      setSubdomainError('Subdomain must be at least 3 characters');
      return;
    }

    if (!/^[a-z0-9-]+$/.test(subdomain)) {
      setSubdomainError('Subdomain can only contain lowercase letters, numbers, and hyphens');
      return;
    }

    setIsCheckingSubdomain(true);
    try {
      const isAvailable = await teamsApi.checkSubdomainAvailability(subdomain);
      if (!isAvailable) {
        setSubdomainError('This subdomain is already taken');
      } else {
        setSubdomainError('');
      }
    } catch (error) {
      console.error('Error checking subdomain:', error);
      setSubdomainError('Unable to check subdomain availability');
    } finally {
      setIsCheckingSubdomain(false);
    }
  };

  const checkEmailAvailability = async (email: string) => {
    if (!email || !/\S+@\S+\.\S+/.test(email)) {
      setEmailError('Please enter a valid email address');
      return;
    }

    setIsCheckingEmail(true);
    try {
      // Note: This would need to be implemented in the backend API
      // For now, we'll just validate the email format
      setEmailError('');
    } catch (error) {
      console.error('Error checking email:', error);
      setEmailError('Unable to check email availability');
    } finally {
      setIsCheckingEmail(false);
    }
  };

  const validateForm = (): string | null => {
    if (!formData.teamName.trim()) return 'Team name is required';
    if (!formData.subdomain.trim()) return 'Subdomain is required';
    if (subdomainError) return 'Please fix the subdomain error';
    if (!formData.ownerFirstName.trim()) return 'First name is required';
    if (!formData.ownerLastName.trim()) return 'Last name is required';
    if (!formData.ownerEmail.trim()) return 'Email is required';
    if (emailError) return 'Please fix the email error';
    if (!formData.ownerPassword) return 'Password is required';
    if (formData.ownerPassword.length < 8) return 'Password must be at least 8 characters';
    if (formData.ownerPassword !== formData.confirmPassword) return 'Passwords do not match';
    
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const dto = {
        name: formData.teamName,
        subdomain: formData.subdomain,
        ownerEmail: formData.ownerEmail,
        ownerFirstName: formData.ownerFirstName,
        ownerLastName: formData.ownerLastName,
        ownerPassword: formData.ownerPassword,
        tier: formData.tier,
        primaryColor: formData.primaryColor,
        secondaryColor: formData.secondaryColor,
        expiresOn: formData.tier === TeamTier.Free ? undefined : 
                   new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString() // 1 year from now
      };
      
      const result = await teamsApi.createTeamWithNewOwner(dto);
      
      // Set form data for success display
      setFormData(prev => ({ 
        ...prev, 
        teamName: result.teamName, 
        subdomain: result.teamSubdomain 
      }));
      setIsSubmitted(true);
      
      // Redirect to team page after a short delay
      setTimeout(() => {
        window.location.href = result.redirectUrl;
      }, 2000);
      
    } catch (error: unknown) {
      console.error('Team creation error:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to create team. Please try again.';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  // Show success message if team creation is submitted
  if (isSubmitted) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-8 text-center">
          <div className="w-16 h-16 bg-green-500 rounded-full flex items-center justify-center mx-auto mb-4">
            <FontAwesomeIcon icon={faCheck} className="text-white text-2xl" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-4">
            Team Created Successfully!
          </h2>
          <p className="text-gray-600 mb-6">
            Your team &quot;{formData.teamName}&quot; has been created. You can now access it at:
          </p>
          <div className="bg-gray-50 rounded-lg p-3 mb-6">
            <code className="text-blue-600 font-mono">
              {formData.subdomain}.teamstride.com
            </code>
          </div>
          <p className="text-sm text-gray-500 mb-6">
            A confirmation email has been sent to {formData.ownerEmail} with your login details.
          </p>
          <button
            onClick={() => window.location.href = `https://${formData.subdomain}.teamstride.com`}
            className="w-full bg-blue-600 text-white py-3 px-4 rounded-md hover:bg-blue-700 transition-colors font-medium"
          >
            Go to Your Team
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">
            Create Your Team
          </h1>
          <p className="text-xl text-gray-600">
            Start managing your running team with TeamStride
          </p>
        </div>

        <div className="bg-white rounded-lg shadow-lg overflow-hidden">
          <div className="p-8">
            {/* Error Alert */}
            {error && (
              <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
                <div className="flex items-start">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mt-0.5 mr-3 flex-shrink-0" />
                  <div className="flex-1">
                    <p className="text-sm text-red-700">{error}</p>
                  </div>
                  <button
                    onClick={() => setError('')}
                    className="text-red-400 hover:text-red-600 ml-2 flex-shrink-0"
                  >
                    <FontAwesomeIcon icon={faTimes} className="w-4 h-4" />
                  </button>
                </div>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-8">
              {/* Plan Selection */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">Choose Your Plan</h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {Object.entries(tierInfo).map(([tierKey, info]) => {
                    const tier = Number(tierKey) as TeamTier;
                    const isSelected = formData.tier === tier;
                    return (
                      <div
                        key={tier}
                        className={`relative border rounded-lg p-4 cursor-pointer transition-all ${
                          isSelected 
                            ? 'border-blue-500 bg-blue-50 ring-2 ring-blue-500' 
                            : 'border-gray-200 hover:border-gray-300'
                        }`}
                        onClick={() => updateFormField('tier', tier)}
                      >
                        {info.popular && (
                          <div className="absolute -top-2 left-1/2 transform -translate-x-1/2">
                            <span className="bg-blue-500 text-white text-xs px-2 py-1 rounded-full flex items-center">
                              <FontAwesomeIcon icon={faStar} className="w-3 h-3 mr-1" />
                              Popular
                            </span>
                          </div>
                        )}
                        <div className="text-center">
                          <h4 className="text-lg font-semibold text-gray-900">{info.name}</h4>
                          <p className="text-2xl font-bold text-blue-600 mt-2">{info.price}</p>
                          <p className="text-sm text-gray-500 mt-1">{info.description}</p>
                          <p className="text-xs text-gray-400 mt-2">{info.limits}</p>
                        </div>
                        <ul className="mt-4 space-y-1">
                          {info.features.map((feature, index) => (
                            <li key={index} className="text-xs text-gray-600 flex items-center">
                              <FontAwesomeIcon icon={faCheck} className="w-3 h-3 text-green-500 mr-2 flex-shrink-0" />
                              {feature}
                            </li>
                          ))}
                        </ul>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Team Information */}
              <div className="border-t border-gray-200 pt-8">
                <h3 className="text-lg font-medium text-gray-900 mb-4">Team Information</h3>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label htmlFor="teamName" className="block text-sm font-medium text-gray-700 mb-1">
                      Team Name *
                    </label>
                    <input
                      id="teamName"
                      name="teamName"
                      type="text"
                      required
                      value={formData.teamName}
                      onChange={(e) => updateFormField('teamName', e.target.value)}
                      className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                      placeholder="e.g., Eagles Running Club"
                      disabled={isLoading}
                    />
                  </div>
                  
                  <div>
                    <label htmlFor="subdomain" className="block text-sm font-medium text-gray-700 mb-1">
                      Team Subdomain *
                    </label>
                    <div className="relative">
                      <input
                        id="subdomain"
                        name="subdomain"
                        type="text"
                        required
                        value={formData.subdomain}
                        onChange={(e) => {
                          const value = e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '');
                          updateFormField('subdomain', value);
                        }}
                        onBlur={(e) => checkSubdomainAvailability(e.target.value)}
                        className={`w-full px-3 py-3 border rounded-lg focus:outline-none focus:ring-2 transition-colors pr-10 ${
                          subdomainError 
                            ? 'border-red-300 focus:ring-red-500 focus:border-red-500' 
                            : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
                        }`}
                        placeholder="eagles-running"
                        disabled={isLoading}
                      />
                      {isCheckingSubdomain && (
                        <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                        </div>
                      )}
                    </div>
                    <p className="mt-1 text-sm text-gray-500">
                      Your team will be available at: <strong>{formData.subdomain || 'your-team'}.teamstride.com</strong>
                    </p>
                    {subdomainError && (
                      <p className="mt-1 text-sm text-red-600">{subdomainError}</p>
                    )}
                  </div>
                </div>

                {/* Team Colors */}
                <div className="mt-6">
                  <h4 className="text-sm font-medium text-gray-700 mb-3">Team Colors</h4>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label htmlFor="primaryColor" className="block text-sm font-medium text-gray-700 mb-1">
                        Primary Color
                      </label>
                      <div className="flex items-center space-x-3">
                        <input
                          id="primaryColor"
                          name="primaryColor"
                          type="color"
                          value={formData.primaryColor}
                          onChange={(e) => updateFormField('primaryColor', e.target.value)}
                          className="w-12 h-10 border border-gray-300 rounded cursor-pointer"
                          disabled={isLoading}
                        />
                        <input
                          type="text"
                          value={formData.primaryColor}
                          onChange={(e) => updateFormField('primaryColor', e.target.value)}
                          className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                          placeholder="#10B981"
                          disabled={isLoading}
                        />
                      </div>
                    </div>
                    
                    <div>
                      <label htmlFor="secondaryColor" className="block text-sm font-medium text-gray-700 mb-1">
                        Secondary Color
                      </label>
                      <div className="flex items-center space-x-3">
                        <input
                          id="secondaryColor"
                          name="secondaryColor"
                          type="color"
                          value={formData.secondaryColor}
                          onChange={(e) => updateFormField('secondaryColor', e.target.value)}
                          className="w-12 h-10 border border-gray-300 rounded cursor-pointer"
                          disabled={isLoading}
                        />
                        <input
                          type="text"
                          value={formData.secondaryColor}
                          onChange={(e) => updateFormField('secondaryColor', e.target.value)}
                          className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                          placeholder="#F0FDF4"
                          disabled={isLoading}
                        />
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Team Owner Information */}
              <div className="border-t border-gray-200 pt-8">
                <h3 className="text-lg font-medium text-gray-900 mb-4">Team Owner Information</h3>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label htmlFor="ownerFirstName" className="block text-sm font-medium text-gray-700 mb-1">
                      First Name *
                    </label>
                    <input
                      id="ownerFirstName"
                      name="ownerFirstName"
                      type="text"
                      required
                      value={formData.ownerFirstName}
                      onChange={(e) => updateFormField('ownerFirstName', e.target.value)}
                      className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                      disabled={isLoading}
                    />
                  </div>
                  
                  <div>
                    <label htmlFor="ownerLastName" className="block text-sm font-medium text-gray-700 mb-1">
                      Last Name *
                    </label>
                    <input
                      id="ownerLastName"
                      name="ownerLastName"
                      type="text"
                      required
                      value={formData.ownerLastName}
                      onChange={(e) => updateFormField('ownerLastName', e.target.value)}
                      className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                      disabled={isLoading}
                    />
                  </div>
                </div>
                
                <div className="mt-6">
                  <label htmlFor="ownerEmail" className="block text-sm font-medium text-gray-700 mb-1">
                    Email Address *
                  </label>
                  <div className="relative">
                    <input
                      id="ownerEmail"
                      name="ownerEmail"
                      type="email"
                      required
                      value={formData.ownerEmail}
                      onChange={(e) => updateFormField('ownerEmail', e.target.value)}
                      onBlur={(e) => checkEmailAvailability(e.target.value)}
                      className={`w-full px-3 py-3 border rounded-lg focus:outline-none focus:ring-2 transition-colors pr-10 ${
                        emailError 
                          ? 'border-red-300 focus:ring-red-500 focus:border-red-500' 
                          : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
                      }`}
                      disabled={isLoading}
                    />
                    {isCheckingEmail && (
                      <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                      </div>
                    )}
                  </div>
                  {emailError && (
                    <p className="mt-1 text-sm text-red-600">{emailError}</p>
                  )}
                </div>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
                  <div>
                    <label htmlFor="ownerPassword" className="block text-sm font-medium text-gray-700 mb-1">
                      Password *
                    </label>
                    <div className="relative">
                      <input
                        id="ownerPassword"
                        name="ownerPassword"
                        type={showPassword ? 'text' : 'password'}
                        required
                        value={formData.ownerPassword}
                        onChange={(e) => updateFormField('ownerPassword', e.target.value)}
                        className="w-full px-3 py-3 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                        disabled={isLoading}
                      />
                      <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
                        disabled={isLoading}
                      >
                        <FontAwesomeIcon 
                          icon={showPassword ? faEyeSlash : faEye} 
                          className="h-5 w-5" 
                        />
                      </button>
                    </div>
                    <p className="mt-1 text-sm text-gray-500">
                      Must be at least 8 characters
                    </p>
                  </div>
                  
                  <div>
                    <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                      Confirm Password *
                    </label>
                    <div className="relative">
                      <input
                        id="confirmPassword"
                        name="confirmPassword"
                        type={showConfirmPassword ? 'text' : 'password'}
                        required
                        value={formData.confirmPassword}
                        onChange={(e) => updateFormField('confirmPassword', e.target.value)}
                        className="w-full px-3 py-3 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                        disabled={isLoading}
                      />
                      <button
                        type="button"
                        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                        className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
                        disabled={isLoading}
                      >
                        <FontAwesomeIcon 
                          icon={showConfirmPassword ? faEyeSlash : faEye} 
                          className="h-5 w-5" 
                        />
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              {/* Submit Button */}
              <div className="border-t border-gray-200 pt-8">
                <button
                  type="submit"
                  disabled={isLoading || !!subdomainError || !!emailError}
                  className="w-full flex justify-center py-4 px-6 border border-transparent rounded-lg shadow-sm text-lg font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isLoading ? (
                    <>
                      <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-3"></div>
                      Creating Your Team...
                    </>
                  ) : (
                    'Create Team'
                  )}
                </button>
                
                <p className="mt-4 text-sm text-gray-500 text-center">
                  By creating a team, you agree to our Terms of Service and Privacy Policy.
                </p>
                
                <div className="mt-6 text-center">
                  <p className="text-sm text-gray-600">
                    Already have an account?{' '}
                    <a 
                      href="/login" 
                      className="font-medium text-blue-600 hover:text-blue-500 transition-colors"
                    >
                      Sign in here
                    </a>
                  </p>
                  <p className="text-sm text-gray-600 mt-2">
                    Looking to join an existing team?{' '}
                    <a 
                      href="/join-team" 
                      className="font-medium text-blue-600 hover:text-blue-500 transition-colors"
                    >
                      Find your team
                    </a>
                  </p>
                </div>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
} 