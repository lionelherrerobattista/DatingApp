import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject, Observable, take } from 'rxjs';
import { Photo } from 'src/app/_models/photo';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  photos: Photo[] = [];
  constructor(private adminService: AdminService, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.loadPhotos();
  }
  
  loadPhotos(){
    this.adminService.getPhotosForApproval().pipe(take(1)).subscribe({
      next: (photos) => {
        this.photos = photos;
      }
    });
  }

  approvePhoto(photoId: number) {
    return this.adminService.approvePhoto(photoId).subscribe({
      next: (photo) => {
        this.photos.splice(this.photos.findIndex(p => p.id === photo.id));
        this.toastr.success("Photo was approved");
      },
      error: (error) => console.log(error)
    })
  }

  rejectPhoto(photoId: number) {
    return this.adminService.rejectPhoto(photoId).subscribe({
      next: (photo) => {
        this.photos.splice(this.photos.findIndex(p => p.id === photo.id));
        this.toastr.success("Photo was rejected");
      },
      error: (error) => console.log(error)
    })
  }

}
