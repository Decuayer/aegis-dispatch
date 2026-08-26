class UpdateProfileRequestModel {
  final String firstName;
  final String lastName;
  final String phone;
  final String department;
  final String? subRole;
  final String? avatarUrl;

  const UpdateProfileRequestModel({
    required this.firstName,
    required this.lastName,
    required this.phone,
    required this.department,
    this.subRole,
    this.avatarUrl,
  });

  Map<String, dynamic> toJson() {
    return {
      'firstName': firstName.trim(),
      'lastName': lastName.trim(),
      'phone': phone.trim(),
      'department': department.trim(),
      'subRole': subRole?.trim(),
      'avatarUrl': avatarUrl,
    };
  }
}
